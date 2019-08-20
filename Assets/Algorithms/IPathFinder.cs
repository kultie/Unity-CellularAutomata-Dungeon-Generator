using System;
using System.Collections.Generic;
using System.Text;
using Algorithms;
using System.Drawing;
using UnityEngine;

namespace Algorithms
{
    [Author("Franco, Gustavo")]
    interface IPathFinder
    {
        #region Events
        event PathFinderDebugHandler PathFinderDebug;
        #endregion

        #region Properties
        bool Stopped
        {
            get;
        }

        HeuristicFormula Formula
        {
            get;
            set;
        }

        bool Diagonals
        {
            get;
            set;
        }

        bool HeavyDiagonals
        {
            get;
            set;
        }

        int HeuristicEstimate
        {
            get;
            set;
        }

        bool PunishChangeDirection
        {
            get;
            set;
        }

        bool TieBreaker
        {
            get;
            set;
        }

        int SearchLimit
        {
            get;
            set;
        }

        double CompletedTime
        {
            get;
            set;
        }

        bool DebugProgress
        {
            get;
            set;
        }

        bool DebugFoundPath
        {
            get;
            set;
        }
        #endregion

        #region Methods
        void FindPathStop();
        List<PathFinderNode> FindPath(Vector2 start, Vector2 end);
        #endregion

    }
}
