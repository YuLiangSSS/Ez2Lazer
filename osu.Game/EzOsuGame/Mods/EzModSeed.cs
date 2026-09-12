// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using osu.Framework.Bindables;
using osu.Framework.Utils;
using osu.Game.Rulesets.Mods;

namespace osu.Game.EzOsuGame.Mods
{
    /// <summary>
    /// Resolves nullable <see cref="IHasSeed"/>-style <c>Seed</c> bindables for beatmap conversion.
    /// </summary>
    /// <remarks>
    /// A null <c>Seed</c> means "the user did not pick a seed": a random seed is rolled for the
    /// conversion instead.
    /// The rolled value is deliberately <b>never</b> written back into the bindable, so features which
    /// convert beatmaps just for display (song select beatmap preview, count statistics, live ChartDan
    /// analysis, difficulty display, ...) can no longer fill the mod settings seed text box.
    /// The rolled value is cached per bindable, so a single conversion - which can resolve the same
    /// seed from several places (converter stage, after-conversion stage, ...) - always uses the same
    /// value.
    /// Gameplay copies get a concrete seed via <see cref="MaterialiseSeeds"/> so the resulting score
    /// records it and replays / score recalculation reproduce exactly the same randomised beatmap.
    /// </remarks>
    public static class EzModSeed
    {
        private static readonly ConditionalWeakTable<Bindable<int?>, ResolvedSeed> resolvedSeeds = new ConditionalWeakTable<Bindable<int?>, ResolvedSeed>();

        /// <summary>
        /// Returns the seed to use for this conversion: the value chosen by the user when present,
        /// otherwise a random seed rolled once for <paramref name="seed"/>.
        /// </summary>
        /// <remarks>
        /// This never writes to <paramref name="seed"/>, so callers converting with the mods that are
        /// bound to the mod settings UI cannot change what the user sees in the seed text box.
        /// </remarks>
        public static int Resolve(Bindable<int?> seed)
        {
            ArgumentNullException.ThrowIfNull(seed);

            if (seed.Value is int explicitSeed)
                return explicitSeed;

            var cached = resolvedSeeds.GetOrCreateValue(seed);

            lock (cached.Gate)
            {
                // The user may have set a value while this thread was waiting for the lock.
                if (seed.Value is int nowSet)
                    return nowSet;

                if (cached.Value is int cachedSeed)
                    return cachedSeed;

                int generated = RNG.Next();

                if (generated == 0)
                    generated = 1;

                cached.Value = generated;
                return generated;
            }
        }

        /// <summary>
        /// Writes the seeds that <see cref="Resolve"/> rolled for <paramref name="mods"/> into their
        /// <c>Seed</c> bindables.
        /// </summary>
        /// <remarks>
        /// Call this on short-lived copies after their beatmap conversion finished (for example the
        /// gameplay clones in <see cref="osu.Game.Screens.Play.Player"/>), so that the score records the
        /// seeds that were actually used and replays / score recalculation reproduce the same beatmap.
        /// Mods which did not resolve a seed during the conversion are left untouched, so "no seed"
        /// keeps whatever meaning each mod gives it.
        /// Never call this for the mods bound to the mod settings UI, as it would fill the seed text box.
        /// </remarks>
        public static void MaterialiseSeeds(IEnumerable<Mod>? mods)
        {
            if (mods == null)
                return;

            foreach (var mod in mods)
            {
                if (mod is not IHasSeed hasSeed)
                    continue;

                var seed = hasSeed.Seed;

                if (seed.Value is not null || !resolvedSeeds.TryGetValue(seed, out var cached))
                    continue;

                int? rolled;

                lock (cached.Gate)
                {
                    rolled = cached.Value;
                    cached.Value = null;
                }

                if (rolled is int value)
                    seed.Value = value;

                resolvedSeeds.Remove(seed);
            }
        }

        private sealed class ResolvedSeed
        {
            public readonly object Gate = new object();
            public int? Value;
        }
    }
}
