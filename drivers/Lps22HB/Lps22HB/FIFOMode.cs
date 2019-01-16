namespace Lps22HB
{
    /// <summary>
    /// Check application note AN4833 for FIFO mode description <ref>https://bit.ly/2VRpdia</ref>
    /// </summary>
    public enum FIFOMode
    {
        Bypass = 0,
        FIFO,
        Stream,
        StreamToFifo,
        BypassToStream,
        BypassToFifo,
        DynamicStream
    }
}
