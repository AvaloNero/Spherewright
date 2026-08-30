using BepInEx.Logging;

namespace Spherewright.Plugin.Game;

internal sealed class ResearchResultAutoAcknowledger
{
    private readonly bool _enabled;
    private readonly ManualLogSource _logger;

    public ResearchResultAutoAcknowledger(bool enabled, ManualLogSource logger)
    {
        _enabled = enabled;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void UpdateOnMainThread()
    {
        if (!_enabled)
        {
            return;
        }

        var resultWindow = UIRoot.instance?.uiGame?.researchResultTip;
        if (resultWindow is null || !resultWindow.active || !resultWindow.ready)
        {
            return;
        }

        // DSP routes both the confirm button and Escape through FadeOut(). Its
        // normal update then closes the window and runs _OnClose(), including
        // GameScenarioLogic.NotifyTechResult for the displayed technology.
        resultWindow.FadeOut();
        _logger.LogDebug("Spherewright acknowledged a ready DSP research-result window through its native FadeOut flow.");
    }
}
