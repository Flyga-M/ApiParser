using System;

namespace ApiParser.V2.Endpoint
{
    [Flags]
    public enum ClientType
    {
        None = 0,
        Blob = 1,
        BulkExpandable = 2,
        AllExpandable = 4
    }
}
