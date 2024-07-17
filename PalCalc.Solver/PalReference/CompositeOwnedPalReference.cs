﻿using PalCalc.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.Solver.PalReference
{
    /// <summary>
    /// Represents a pair of male and female instances of the same pal. This allows us to represent
    /// Male+Female owned pals to act as "wildcard" genders. (Without this the solver will tend to prefer
    /// redundantly breeding a pal of "opposite gender" compared to another pal step which has lots of
    /// requirements + breeding attempts. It wouldn't directly pair it with a male or female pal, since
    /// that would require breeding the "difficult" pal to have a specific gender.)
    /// 
    /// These pals _should_, but are not _guaranteed_, to have the same set of traits:
    /// 
    /// - If two pals have different desired traits, they should NOT be made composite.
    /// - Conversely, if one pal has a desired trait, both pals will have that desired trait.
    /// - The traits for this reference will match whichever pal has the most traits.
    /// </summary>
    public class CompositeOwnedPalReference : IPalReference
    {
        public CompositeOwnedPalReference(OwnedPalReference male, OwnedPalReference female)
        {
            Male = male;
            Female = female;

            if (male.IV_HP + male.IV_Shot + male.IV_Defense > female.IV_HP + female.IV_Shot + female.IV_Defense)
            {
                IV_HP = male.IV_HP;
                IV_Shot = male.IV_Shot;
                IV_Defense = male.IV_Defense;
            }
            else
            {
                IV_HP = female.IV_HP;
                IV_Shot = female.IV_Shot;
                IV_Defense = female.IV_Defense;
            }

            Location = new CompositeRefLocation(male.Location, female.Location);

            // effective traits based on which pal has the most irrelevant traits
            EffectiveTraits = male.EffectiveTraits.Count > female.EffectiveTraits.Count ? male.EffectiveTraits : female.EffectiveTraits;
            EffectiveTraitsHash = EffectiveTraits.SetHash();

            ActualTraits = Male.ActualTraits.Intersect(Female.ActualTraits).ToList();
            while (ActualTraits.Count < EffectiveTraits.Count) ActualTraits.Add(new RandomTrait());
        }

        public OwnedPalReference Male { get; }
        public OwnedPalReference Female { get; }

        public Pal Pal => Male.Pal;

        public List<Trait> EffectiveTraits { get; private set; }

        public int EffectiveTraitsHash { get; private set; }

        public List<Trait> ActualTraits { get; }

        public PalGender Gender { get; private set; } = PalGender.WILDCARD;

        public int NumTotalBreedingSteps { get; } = 0;

        public IPalRefLocation Location { get; }

        public TimeSpan BreedingEffort { get; } = TimeSpan.Zero;

        public TimeSpan SelfBreedingEffort { get; } = TimeSpan.Zero;

        public int IV_HP { get; }
        public int IV_Shot { get; }
        public int IV_Defense { get; }

        private CompositeOwnedPalReference oppositeWildcardReference;
        public IPalReference WithGuaranteedGender(PalDB db, PalGender gender)
        {
            switch (gender)
            {
                case PalGender.MALE: return Male;
                case PalGender.FEMALE: return Female;
                case PalGender.WILDCARD: return this;
                case PalGender.OPPOSITE_WILDCARD:
                    if (oppositeWildcardReference == null)
                        oppositeWildcardReference = new CompositeOwnedPalReference(Male, Female) { Gender = gender };
                    return oppositeWildcardReference;

                default: throw new NotImplementedException();
            }
        }

        // TODO - maybe just use Pal, TraitsHash, Gender? don't need hashes specific to the instances chosen?
        public override int GetHashCode() =>
            HashCode.Combine(nameof(CompositeOwnedPalReference), Male, Female, EffectiveTraitsHash, Gender);
    }
}
