﻿using PalCalc.Solver.PalReference;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.Solver.ResultPruning
{
    public class MinimumWildPalsPruning : IResultPruning
    {
        public MinimumWildPalsPruning(CancellationToken token) : base(token)
        {
        }

        public override IEnumerable<IPalReference> Apply(IEnumerable<IPalReference> results) =>
            FirstGroupOf(results, r => r.AllReferences().Count(p => p is WildPalReference));
    }
}
