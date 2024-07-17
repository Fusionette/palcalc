﻿using PalCalc.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.Solver.PalReference
{
    public class BredPalReference : IPalReference
    {
        private GameSettings gameSettings;

        public int IV_HP { get; }
        public int IV_Shot { get; }
        public int IV_Defense { get; }

        private BredPalReference(GameSettings gameSettings, Pal pal, IPalReference parent1, IPalReference parent2, List<Trait> traits)
        {
            this.gameSettings = gameSettings;

            Pal = pal;
            if (parent1.Pal.InternalIndex > parent2.Pal.InternalIndex)
            {
                Parent1 = parent1;
                Parent2 = parent2;
            }
            else if (parent1.Pal.InternalIndex < parent2.Pal.InternalIndex)
            {
                Parent1 = parent2;
                Parent2 = parent1;
            }
            else if (parent1.GetHashCode() < parent2.GetHashCode())
            {
                Parent1 = parent1;
                Parent2 = parent2;
            }
            else
            {
                Parent1 = parent2;
                Parent2 = parent1;
            }

            IV_HP = int.Max(parent1.IV_HP, parent2.IV_HP);
            IV_Shot = int.Max(parent1.IV_Shot, parent2.IV_Shot);
            IV_Defense = int.Max(parent1.IV_Defense, parent2.IV_Defense);

            EffectiveTraits = traits;
            EffectiveTraitsHash = traits.SetHash();

            parentBreedingEffort = gameSettings.MultipleBreedingFarms && Parent1 is BredPalReference && Parent2 is BredPalReference
                ? Parent1.BreedingEffort > Parent2.BreedingEffort
                    ? Parent1.BreedingEffort
                    : Parent2.BreedingEffort
                : Parent1.BreedingEffort + Parent2.BreedingEffort;
        }

        public BredPalReference(GameSettings gameSettings, Pal pal, IPalReference parent1, IPalReference parent2, List<Trait> traits, float traitsProbability) : this(gameSettings, pal, parent1, parent2, traits)
        {
            Gender = PalGender.WILDCARD;
            if (traitsProbability <= 0) AvgRequiredBreedings = int.MaxValue;
            else AvgRequiredBreedings = (int)Math.Ceiling(1.0f / traitsProbability);

            TraitsProbability = traitsProbability;
        }

        public float TraitsProbability { get; private set; }

        public Pal Pal { get; private set; }
        public IPalReference Parent1 { get; private set; }
        public IPalReference Parent2 { get; private set; }

        public PalGender Gender { get; private set; } = PalGender.WILDCARD;

        public IPalRefLocation Location => BredRefLocation.Instance;

        public int AvgRequiredBreedings { get; private set; }
        public TimeSpan SelfBreedingEffort => AvgRequiredBreedings * gameSettings.AvgBreedingTime;

        private TimeSpan parentBreedingEffort;
        public TimeSpan BreedingEffort => SelfBreedingEffort + parentBreedingEffort;

        private int numTotalBreedingSteps = -1;
        public int NumTotalBreedingSteps
        {
            get
            {
                if (numTotalBreedingSteps < 0)
                    numTotalBreedingSteps = 1 + Parent1.NumTotalBreedingSteps + Parent2.NumTotalBreedingSteps;

                return numTotalBreedingSteps;
            }
        }

        public List<Trait> EffectiveTraits { get; }

        public int EffectiveTraitsHash { get; }

        public List<Trait> ActualTraits => EffectiveTraits;

        private IPalReference WithGuaranteedGenderImpl(PalDB db, PalGender gender)
        {
            if (gender == PalGender.WILDCARD)
            {
                return this;
            }
            else if (gender == PalGender.OPPOSITE_WILDCARD)
            {
                // should only happen if the other parent has the same gender probabilities as this parent
                if (db.BreedingMostLikelyGender[Pal] != PalGender.WILDCARD)
                {
                    // assume that the other parent has the more likely gender
                    return new BredPalReference(gameSettings, Pal, Parent1, Parent2, EffectiveTraits)
                    {
                        AvgRequiredBreedings = (int)Math.Ceiling(AvgRequiredBreedings / db.BreedingGenderProbability[Pal][db.BreedingLeastLikelyGender[Pal]]),
                        Gender = gender,
                        TraitsProbability = TraitsProbability,
                    };
                }
                else
                {
                    // no preferred bred gender, i.e. 50/50 bred chance, so have half the probability / twice the effort to get desired instance
                    return new BredPalReference(gameSettings, Pal, Parent1, Parent2, EffectiveTraits)
                    {
                        AvgRequiredBreedings = AvgRequiredBreedings * 2,
                        Gender = gender,
                        TraitsProbability = TraitsProbability,
                    };
                }
            }
            else
            {
                var genderProbability = db.BreedingGenderProbability[Pal][gender];
                return new BredPalReference(gameSettings, Pal, Parent1, Parent2, EffectiveTraits)
                {
                    AvgRequiredBreedings = (int)Math.Ceiling(AvgRequiredBreedings / genderProbability),
                    Gender = gender,
                    TraitsProbability = TraitsProbability,
                };
            }
        }

        private ConcurrentDictionary<PalGender, IPalReference> cachedGuaranteedGenders = null;
        public IPalReference WithGuaranteedGender(PalDB db, PalGender gender)
        {
            if (Gender != PalGender.WILDCARD) throw new Exception("Cannot change gender of bred pal with an already-guaranteed gender");

            if (cachedGuaranteedGenders == null) cachedGuaranteedGenders = new ConcurrentDictionary<PalGender, IPalReference>();

            return cachedGuaranteedGenders.GetOrAdd(gender, (gender) => WithGuaranteedGenderImpl(db, gender));
        }

        public override string ToString() => $"Bred {Gender} {Pal} w/ ({EffectiveTraits.TraitsListToString()})";

        public override bool Equals(object obj)
        {
            var asBred = obj as BredPalReference;
            if (ReferenceEquals(asBred, null)) return false;

            return GetHashCode() == obj.GetHashCode();
        }

        public override int GetHashCode() => HashCode.Combine(
            nameof(BredPalReference),
            Pal,
            Parent1.GetHashCode() ^ Parent2.GetHashCode(),
            EffectiveTraitsHash,
            BreedingEffort,
            SelfBreedingEffort,
            Gender
        );
    }
}
