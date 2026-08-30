namespace Spherewright.Bridge.Core.Framing;

public sealed class FrameProtocolException : Exception
{
    public FrameProtocolException(string message)
        : base(message)
    {
    }

    public FrameProtocolException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

