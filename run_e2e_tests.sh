#!/bin/bash

# E2E Test Orchestration Script for XcavateProfile
# Manages the full test cycle: start database, start API, run tests, cleanup

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Configuration
POSTGRES_CONTAINER="xcavate-postgres"
API_CONTAINER="xcavate-api"
COMPOSE_PROJECT_NAME="xcavate"
TEST_REPORTS_DIR="test-reports"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Get the dotnet global tools path
get_dotnet_tools_path() {
    local dotnet_root
    dotnet_root=$(dotnet --list-sdks 2>/dev/null | head -1)
    if [ -z "$dotnet_root" ]; then
        # Try to find .dotnet/tools in common locations
        if [ -d "$HOME/.dotnet/tools" ]; then
            echo "$HOME/.dotnet/tools"
        elif [ -d "/root/.dotnet/tools" ]; then
            echo "/root/.dotnet/tools"
        else
            echo "$HOME/.dotnet/tools"
        fi
    else
        echo "$HOME/.dotnet/tools"
    fi
}

cleanup() {
    log_info "Cleaning up containers..."
    
    # Stop and remove containers
    if docker ps -q --filter name="$COMPOSE_PROJECT_NAME" | grep -q .; then
        docker-compose -p $COMPOSE_PROJECT_NAME down -v 2>/dev/null || true
    fi
    
    # Remove networks
    docker network rm ${COMPOSE_PROJECT_NAME}_default 2>/dev/null || true
    
    # Clean up test reports directory
    mkdir -p "$TEST_REPORTS_DIR"
}

# Trap to ensure cleanup on exit
trap cleanup EXIT

main() {
    log_info "Starting E2E test orchestration..."
    
    # Step 1: Start PostgreSQL database
    log_info "Step 1: Starting PostgreSQL database..."
    docker-compose -p $COMPOSE_PROJECT_NAME up -d postgres
    
    # Step 2: Wait for PostgreSQL to be ready
    log_info "Step 2: Waiting for PostgreSQL to be ready..."
    local max_attempts=30
    local attempt=1
    while [ $attempt -le $max_attempts ]; do
        if docker exec $POSTGRES_CONTAINER pg_isready -U xcavate_user -d xcavate_profile >/dev/null 2>&1; then
            log_info "PostgreSQL is ready!"
            break
        fi
        log_info "PostgreSQL not ready yet (attempt $attempt/$max_attempts)..."
        sleep 2
        attempt=$((attempt + 1))
    done
    
    if [ $attempt -gt $max_attempts ]; then
        log_error "PostgreSQL failed to become ready after $max_attempts attempts"
        exit 1
    fi
    
    # Step 3: Start the API server in background
    log_info "Step 3: Starting API server..."
    
    # Build the API
    log_info "Building API project..."
    dotnet build src/XcavateProfileApi/XcavateProfileApi.csproj -c Release
    
    # Run database migrations
    log_info "Running database migrations..."
    # Get the dotnet global tools path and ensure it's in PATH
    DOTNET_TOOLS_PATH=$(get_dotnet_tools_path)
    export PATH="$PATH:$DOTNET_TOOLS_PATH"

    # Build the project first to ensure binaries exist
    dotnet build src/XcavateProfileApi/XcavateProfileApi.csproj -c Release >/dev/null

    local postgres_port="${POSTGRES_PORT:-5432}"

    # Set connection string - use localhost since we're running migrations from the host
    export POSTGRES_HOST="localhost"
    export POSTGRES_PORT="${postgres_port}"
    export POSTGRES_DB="${POSTGRES_DB:-xcavate_profile}"
    export POSTGRES_USER="${POSTGRES_USER:-xcavate_user}"
    export POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-12345678}"

    # Set ConnectionStrings__Default for EF Core design-time operations
    export ConnectionStrings__Default="Host=${POSTGRES_HOST};Port=${POSTGRES_PORT};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"

    log_info "Connection string: ${ConnectionStrings__Default}"

    dotnet ef database update --project src/XcavateProfileApi --context ProfileDbContext

    # Kill any existing process on port 5000 to avoid "address already in use" error
    log_info "Checking for existing processes on port 5000..."
    EXISTING_PID=$(lsof -t -i:5000 2>/dev/null || netstat -ano 2>/dev/null | grep 5000 | head -1 | awk '{print $NF}' || true)
    if [ -n "$EXISTING_PID" ]; then
        log_info "Killing existing process on port 5000: $EXISTING_PID"
        kill $EXISTING_PID 2>/dev/null || true
        sleep 1
    fi

    # Start the API server in background
    cd src/XcavateProfileApi
    dotnet run --urls "http://0.0.0.0:5000" &
    API_PID=$!
    cd - > /dev/null
    
    # Wait for API to be ready
    log_info "Waiting for API to be ready..."
    sleep 5
    max_attempts=15
    attempt=1
    while [ $attempt -le $max_attempts ]; do
        if curl -s http://localhost:5000/swagger/v1/swagger.json >/dev/null 2>&1; then
            log_info "API is ready!"
            break
        fi
        log_info "API not ready yet (attempt $attempt/$max_attempts)..."
        sleep 2
        attempt=$((attempt + 1))
    done
    
    if [ $attempt -gt $max_attempts ]; then
        log_error "API failed to become ready after $max_attempts attempts"
        kill $API_PID 2>/dev/null || true
        exit 1
    fi
    
    # Step 4: Run E2E tests
    log_info "Step 4: Running E2E tests..."
    
    # Create test results directory
    mkdir -p "$TEST_REPORTS_DIR"
    
    # Run tests with NUnit
    dotnet test tests/XcavateProfile.ApiTests/XcavateProfile.ApiTests.csproj \
        --configuration Release \
        --results-directory "$TEST_REPORTS_DIR" \
        --logger "console;verbosity=detailed" \
        --logger "html;logfilename=test_results.html;outputdirectory=$TEST_REPORTS_DIR"
    
    TEST_EXIT_CODE=$?
    
    # Step 5: Cleanup
    log_info "Step 5: Cleanup..."
    
    # Stop API server
    kill $API_PID 2>/dev/null || true
    
    # Stop Docker containers
    docker-compose -p $COMPOSE_PROJECT_NAME down -v
    
    # Report results
    if [ $TEST_EXIT_CODE -eq 0 ]; then
        log_info "All E2E tests passed!"
        exit 0
    else
        log_error "Some E2E tests failed!"
        exit $TEST_EXIT_CODE
    fi
}

# Run main function
main "$@"
