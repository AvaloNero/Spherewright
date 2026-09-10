using System.Globalization;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>Failure text only; never changes candidate order, native checks or plan eligibility.</summary>
public sealed class ExactInserterRejectionReport
{
    public const int MaximumReasonCharacters = 512;
    private string _lastPreNativeRejection = "none";
    private string _lastNativeRejection = "none";

    public int Attempts { get; private set; }
    public int NativeChecks { get; private set; }

    public void Record(string? rejection, bool nativeCheckPerformed)
    {
        Attempts++;
        var reason = string.IsNullOrWhiteSpace(rejection) ? "unknown" : rejection!;
        if (reason.Length > MaximumReasonCharacters)
            reason = reason.Substring(0, MaximumReasonCharacters);
        if (nativeCheckPerformed)
        {
            NativeChecks++;
            _lastNativeRejection = reason;
        }
        else
        {
            _lastPreNativeRejection = reason;
        }
    }

    public string DescribeFailure() => string.Format(CultureInfo.InvariantCulture,
        "Exact-slot attempts={0}; nativeChecks={1}; lastNativeRejection={2}; lastPreNativeRejection={3}.",
        Attempts, NativeChecks, _lastNativeRejection, _lastPreNativeRejection);
}
