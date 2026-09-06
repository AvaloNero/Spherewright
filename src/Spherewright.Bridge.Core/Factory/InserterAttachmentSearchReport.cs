using System.Globalization;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>Read-only preparation diagnostics, never a placement approval or plan binding.</summary>
public sealed class InserterAttachmentSearchReport
{
    public int Seeds { get; private set; }
    public int AdmittedSeeds { get; private set; }
    public int Projections { get; private set; }
    public int FacingPairs { get; private set; }
    public int TiltRejections { get; private set; }
    public int CandidateChecks { get; private set; }
    public double? BestFacingDegrees { get; private set; }
    public bool LimitReached { get; private set; }

    public void RecordSeed(bool admitted)
    {
        Seeds++;
        if (admitted) AdmittedSeeds++;
    }

    public void RecordProjection(double? facingDegrees)
    {
        Projections++;
        if (facingDegrees.HasValue && !double.IsNaN(facingDegrees.Value) && !double.IsInfinity(facingDegrees.Value)
            && facingDegrees.Value >= 0 && facingDegrees.Value <= 180
            && (!BestFacingDegrees.HasValue || facingDegrees.Value < BestFacingDegrees.Value))
            BestFacingDegrees = facingDegrees;
    }

    public void RecordFacingPair() => FacingPairs++;
    public void RecordTiltRejection() => TiltRejections++;

    public bool TryRecordCandidateCheck()
    {
        if (CandidateChecks >= InserterBeltAttachmentPolicy.MaximumNativeCandidates)
        {
            LimitReached = true;
            return false;
        }
        CandidateChecks++;
        return true;
    }

    public string FailureStage => LimitReached ? "candidate_limit"
        : CandidateChecks > 0 ? "candidate_validation_rejected"
        : TiltRejections > 0 ? "native_tilt_rejected"
        : Projections > 0 ? "no_facing_interpolated_pair"
        : AdmittedSeeds > 0 ? "no_finite_projection"
        : "no_admissible_seed";

    public string DescribeFailure() => string.Format(CultureInfo.InvariantCulture,
        "Native attachment stage={0}; seeds={1}; admittedSeeds={2}; projections={3}; facingPairs={4}; "
        + "tiltRejections={5}; candidateChecks={6}; bestFacingDegrees={7} (must be <11).",
        FailureStage, Seeds, AdmittedSeeds, Projections, FacingPairs, TiltRejections, CandidateChecks,
        BestFacingDegrees.HasValue ? BestFacingDegrees.Value.ToString("F3", CultureInfo.InvariantCulture) : "unknown");

    public const string Recovery = "Do not repeat the same endpoint pair unchanged. Read the native attachment failure stage: "
        + "geometry_unavailable is not proof of bad angles; no_admissible_seed/no_finite_projection/no_facing_interpolated_pair "
        + "requires a different evidence-backed local geometry. Only an explicit native OutOfReach rejection calls for moving closer. "
        + "For geometry_buffer_busy, wait before one bounded fresh prepare; never reuse an old token. "
        + "Do not relax native angles, retarget nearby belts implicitly, or build a whole unverified route.";
}
