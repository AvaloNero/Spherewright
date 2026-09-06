namespace Spherewright.Contracts.Actions;

public sealed class ResearchQueueReadback
{
    public long CapturedAtGameTick { get; set; }
    public List<int> BeforeQueue { get; set; } = new List<int>();
    public List<int> AfterQueue { get; set; } = new List<int>();
    public int PreviousCurrentTechId { get; set; }
    public int CurrentTechId { get; set; }
    public bool ResearchProgressPreserved { get; set; }
    public bool InventoryPreserved { get; set; }
}
