using System;
using System.Collections.Generic;
using System.Text;
using Spine;

public static class AnimationNameUtility
{
    public static string Normalize(string animationName)
    {
        if (string.IsNullOrWhiteSpace(animationName))
            return string.Empty;

        animationName = animationName.Trim();

        StringBuilder builder = new StringBuilder(animationName.Length);
        bool capitalizeNext = true;

        for (int i = 0; i < animationName.Length; i++)
        {
            char character = animationName[i];

            if (character == ' ' || character == '_' || character == '-')
            {
                capitalizeNext = true;
                continue;
            }

            if (capitalizeNext)
            {
                builder.Append(char.ToUpperInvariant(character));
                capitalizeNext = false;
            }
            else
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }

    public static string ResolveAnimationName(
        IEnumerable<Animation> animations,
        string requestedName
    )
    {
        if (string.IsNullOrWhiteSpace(requestedName))
            return string.Empty;

        if (animations == null)
            return requestedName;

        foreach (Animation animation in animations)
        {
            if (animation == null)
                continue;

            if (string.Equals(
                animation.Name,
                requestedName,
                StringComparison.OrdinalIgnoreCase
            ))
            {
                return animation.Name;
            }
        }

        return requestedName;
    }
}
