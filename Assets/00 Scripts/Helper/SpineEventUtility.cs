using System;
using Spine;

public static class SpineEventUtility
{
    const string AttackKeyword = "attack";

    public static bool IsAttackEvent(Event spineEvent)
    {
        if (spineEvent == null || spineEvent.Data == null || string.IsNullOrEmpty(spineEvent.Data.Name))
            return false;

        return spineEvent.Data.Name.IndexOf(AttackKeyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static float GetTrackDuration(TrackEntry trackEntry)
    {
        if (trackEntry == null || trackEntry.Animation == null)
            return 0f;

        float timeScale = UnityEngine.Mathf.Max(0.01f, trackEntry.TimeScale);
        float duration = trackEntry.AnimationEnd - trackEntry.AnimationStart;

        if (duration <= 0f)
            duration = trackEntry.Animation.Duration;

        return UnityEngine.Mathf.Max(0f, duration) / timeScale;
    }
}
