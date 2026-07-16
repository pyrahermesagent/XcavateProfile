namespace XcavateProfileApiClient
{
    public class EmptyPayloadBody : IPayloadBody
    {
        public string Hash()
        {
            return "";
        }
    }
}
