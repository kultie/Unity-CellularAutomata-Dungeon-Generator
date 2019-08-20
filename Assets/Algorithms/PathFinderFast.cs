//
//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER 
//  REMAINS UNCHANGED.
//
//  Email:  gustavo_franco@hotmail.com
//
//  Copyright (C) 2006 Franco, Gustavo 
//
#define DEBUGON

using System;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Algorithms;
using UnityEngine;

namespace Algorithms
{
    [Author("Franco, Gustavo")]
    public class PathFinderFast : IPathFinder
    {
        #region Structs
        [Author("Franco, Gustavo")]
	    [StructLayout(LayoutKind.Sequential, Pack=1)] 
        internal struct PathFinderNodeFast
        {
            #region Variables Declaration
            public int     F; // f = gone + heuristic
            public int     G;
            public ushort  PX; // Parent
            public ushort  PY;
            public byte    Status;
            #endregion
        }
        #endregion

        #region Events
        public event PathFinderDebugHandler PathFinderDebug;
        #endregion

        #region Variables Declaration
        // Heap variables are initializated to default, but I like to do it anyway
        private byte[,]                         mGrid                   = null;
        private PriorityQueueB<int>             mOpen                   = null;
        private List<PathFinderNode>            mClose                  = new List<PathFinderNode>();
        private bool                            mStop                   = false;
        private bool                            mStopped                = true;
        private int                             mHoriz                  = 0;
        private HeuristicFormula                mFormula                = HeuristicFormula.Manhattan;
        private bool                            mDiagonals              = true;
        private int                             mHEstimate              = 2;
        private bool                            mPunishChangeDirection  = false;
        private bool                            mTieBreaker             = false;
        private bool                            mHeavyDiagonals         = false;
        private int                             mSearchLimit            = 2000;
        private double                          mCompletedTime          = 0;
        private bool                            mDebugProgress          = false;
        private bool                            mDebugFoundPath         = false;
        private PathFinderNodeFast[]            mCalcGrid               = null;
        private byte                            mOpenNodeValue          = 1;
        private byte                            mCloseNodeValue         = 2;
        
        //Promoted local variables to member variables to avoid recreation between calls
        private int                             mH                      = 0;
        private int                             mLocation               = 0;
        private int                             mNewLocation            = 0;
        private ushort                          mLocationX              = 0;
        private ushort                          mLocationY              = 0;
        private ushort                          mNewLocationX           = 0;
        private ushort                          mNewLocationY           = 0;
        private int                             mCloseNodeCounter       = 0;
        private ushort                          mGridX                  = 0;
        private ushort                          mGridY                  = 0;
        private ushort                          mGridXMinus1            = 0;
        private ushort                          mGridYLog2              = 0;
        private bool                            mFound                  = false;
        private sbyte[,]                        mDirection              = new sbyte[8,2]{{0,-1} , {1,0}, {0,1}, {-1,0}, {1,-1}, {1,1}, {-1,1}, {-1,-1}};
        private int                             mEndLocation            = 0;
        private int                             mNewG                   = 0;
        #endregion

        #region Constructors
        public PathFinderFast(byte[,] grid)
        {
            if (grid == null)
                throw new Exception("Grid cannot be null");

            mGrid           = grid;
            mGridX          = (ushort) (mGrid.GetUpperBound(0) + 1);
            mGridY          = (ushort) (mGrid.GetUpperBound(1) + 1);
            mGridXMinus1    = (ushort) (mGridX - 1);
            mGridYLog2      = (ushort) Math.Log(mGridY, 2);

            // This should be done at the constructor, for now we leave it here.
            if (Math.Log(mGridX, 2) != (int) Math.Log(mGridX, 2) ||
                Math.Log(mGridY, 2) != (int) Math.Log(mGridY, 2))
                throw new Exception("Invalid Grid, size in X and Y must be power of 2");

            if (mCalcGrid == null || mCalcGrid.Length != (mGridX * mGridY))
                mCalcGrid = new PathFinderNodeFast[mGridX * mGridY];

            mOpen   = new PriorityQueueB<int>(new ComparePFNodeMatrix(mCalcGrid));
        }
        #endregion

        #region Properties
        public bool Stopped
        {
            get { return mStopped; }
        }

        public HeuristicFormula Formula
        {
            get { return mFormula; }
            set { mFormula = value; }
        }

        public bool Diagonals
        {
            get { return mDiagonals; }
            set 
            { 
                mDiagonals = value; 
                if (mDiagonals)
                    mDirection = new sbyte[8,2]{{0,-1} , {1,0}, {0,1}, {-1,0}, {1,-1}, {1,1}, {-1,1}, {-1,-1}};
                else
                    mDirection = new sbyte[4,2]{{0,-1} , {1,0}, {0,1}, {-1,0}};
            }
        }

        public bool HeavyDiagonals
        {
            get { return mHeavyDiagonals; }
            set { mHeavyDiagonals = value; }
        }

        public int HeuristicEstimate
        {
            get { return mHEstimate; }
            set { mHEstimate = value; }
        }

        public bool PunishChangeDirection
        {
            get { return mPunishChangeDirection; }
            set { mPunishChangeDirection = value; }
        }

        public bool TieBreaker
        {
            get { return mTieBreaker; }
            set { mTieBreaker = value; }
        }

        public int SearchLimit
        {
            get { return mSearchLimit; }
            set { mSearchLimit = value; }
        }

        public double CompletedTime
        {
            get { return mCompletedTime; }
            set { mCompletedTime = value; }
        }

        public bool DebugProgress
        {
            get { return mDebugProgress; }
            set { mDebugProgress = value; }
        }

        public bool DebugFoundPath
        {
            get { return mDebugFoundPath; }
            set { mDebugFoundPath = value; }
        }
        #endregion

        #region Methods
        public void FindPathStop()
        {
            mStop = true;
        }

        public List<PathFinderNode> FindPath(Vector2 start, Vector2 end)
        {
            lock(this)
            {
                HighResolutionTime.Start();

                // Is faster if we don't clear the matrix, just assign different values for open and close and ignore the rest
                // I could have user Array.Clear() but using unsafe code is faster, no much but it is.
                //fixed (PathFinderNodeFast* pGrid = tmpGrid) 
                //    ZeroMemory((byte*) pGrid, sizeof(PathFinderNodeFast) * 1000000);

                mFound              = false;
                mStop               = false;
                mStopped            = false;
                mCloseNodeCounter   = 0;
                mOpenNodeValue      += 2;
                mCloseNodeValue     += 2;
                mOpen.Clear();
                mClose.Clear();

                #if DEBUGON
                if (mDebugProgress && PathFinderDebug != null)
                    PathFinderDebug(0, 0, (int)start.x, (int)start.y, PathFinderNodeType.Start, -1, -1);
                if (mDebugProgress && PathFinderDebug != null)
                    PathFinderDebug(0, 0, (int)end.x, (int)end.y, PathFinderNodeType.End, -1, -1);
                #endif

                mLocation                      = ((int)start.y << mGridYLog2) + (int)start.x;
                mEndLocation                   = ((int)end.y << mGridYLog2) + (int)end.x;
                mCalcGrid[mLocation].G         = 0;
                mCalcGrid[mLocation].F         = mHEstimate;
                mCalcGrid[mLocation].PX        = (ushort) (int)start.x;
                mCalcGrid[mLocation].PY        = (ushort) (int)start.y;
                mCalcGrid[mLocation].Status    = mOpenNodeValue;

                mOpen.Push(mLocation);
                while(mOpen.Count > 0 && !mStop)
                {
                    mLocation    = mOpen.Pop();

                    //Is it in closed list? means this node was already processed
                    if (mCalcGrid[mLocation].Status == mCloseNodeValue)
                        continue;

                    mLocationX   = (ushort) (mLocation & mGridXMinus1);
                    mLocationY   = (ushort) (mLocation >> mGridYLog2);
                    
                    #if DEBUGON
                    if (mDebugProgress && PathFinderDebug != null)
                        PathFinderDebug(0, 0, mLocation & mGridXMinus1, mLocation >> mGridYLog2, PathFinderNodeType.Current, -1, -1);
                    #endif

                    if (mLocation == mEndLocation)
                    {
                        mCalcGrid[mLocation].Status = mCloseNodeValue;
                        mFound = true;
                        break;
                    }

                    if (mCloseNodeCounter > mSearchLimit)
                    {
                        mStopped = true;
                        mCompletedTime = HighResolutionTime.GetTime();
                        return null;
                    }

                    if (mPunishChangeDirection)
                        mHoriz = (mLocationX - mCalcGrid[mLocation].PX); 

                    //Lets calculate each successors
                    for (int i=0; i<(mDiagonals ? 8 : 4); i++)
                    {
                        mNewLocationX = (ushort) (mLocationX + mDirection[i,0]);
                        mNewLocationY = (ushort) (mLocationY + mDirection[i,1]);
                        mNewLocation  = (mNewLocationY << mGridYLog2) + mNewLocationX;

                        if (mNewLocationX >= mGridX || mNewLocationY >= mGridY)
                            continue;

                        // Unbreakeable?
                        if (mGrid[mNewLocationX, mNewLocationY] == 0)
                            continue;

                        if (mHeavyDiagonals && i>3)
                            mNewG = mCalcGrid[mLocation].G + (int) (mGrid[mNewLocationX, mNewLocationY] * 2.41);
                        else
                            mNewG = mCalcGrid[mLocation].G + mGrid[mNewLocationX, mNewLocationY];

                        if (mPunishChangeDirection)
                        {
                            if ((mNewLocationX - mLocationX) != 0)
                            {
                                if (mHoriz == 0)
                                    mNewG += Math.Abs(mNewLocationX - (int)end.x) + Math.Abs(mNewLocationY - (int)end.y);
                            }
                            if ((mNewLocationY - mLocationY) != 0)
                            {
                                if (mHoriz != 0)
                                    mNewG += Math.Abs(mNewLocationX - (int)end.x) + Math.Abs(mNewLocationY - (int)end.y);
                            }
                        }

                        //Is it open or closed?
                        if (mCalcGrid[mNewLocation].Status == mOpenNodeValue || mCalcGrid[mNewLocation].Status == mCloseNodeValue)
                        {
                            // The current node has less code than the previous? then skip this node
                            if (mCalcGrid[mNewLocation].G <= mNewG)
                                continue;
                        }

                        mCalcGrid[mNewLocation].PX      = mLocationX;
                        mCalcGrid[mNewLocation].PY      = mLocationY;
                        mCalcGrid[mNewLocation].G       = mNewG;

                        switch(mFormula)
                        {
                            default:
                            case HeuristicFormula.Manhattan:
                                mH = mHEstimate * (Math.Abs(mNewLocationX - (int)end.x) + Math.Abs(mNewLocationY - (int)end.y));
                                break;
                            case HeuristicFormula.MaxDXDY:
                                mH = mHEstimate * (Math.Max(Math.Abs(mNewLocationX - (int)end.x), Math.Abs(mNewLocationY - (int)end.y)));
                                break;
                            case HeuristicFormula.DiagonalShortCut:
                                int h_diagonal  = Math.Min(Math.Abs(mNewLocationX - (int)end.x), Math.Abs(mNewLocationY - (int)end.y));
                                int h_straight  = (Math.Abs(mNewLocationX - (int)end.x) + Math.Abs(mNewLocationY - (int)end.y));
                                mH = (mHEstimate * 2) * h_diagonal + mHEstimate * (h_straight - 2 * h_diagonal);
                                break;
                            case HeuristicFormula.Euclidean:
                                mH = (int) (mHEstimate * Math.Sqrt(Math.Pow((mNewLocationY - (int)end.x) , 2) + Math.Pow((mNewLocationY - (int)end.y), 2)));
                                break;
                            case HeuristicFormula.EuclideanNoSQR:
                                mH = (int) (mHEstimate * (Math.Pow((mNewLocationX - (int)end.x) , 2) + Math.Pow((mNewLocationY - (int)end.y), 2)));
                                break;                            
                        }
                        if (mTieBreaker)
                        {
                            int dx1 = mLocationX - (int)end.x;
                            int dy1 = mLocationY - (int)end.y;
                            int dx2 = (int)start.x - (int)end.x;
                            int dy2 = (int)start.y - (int)end.y;
                            int cross = Math.Abs(dx1 * dy2 - dx2 * dy1);
                            mH = (int) (mH + cross * 0.001);
                        }
                        mCalcGrid[mNewLocation].F = mNewG + mH;

                        #if DEBUGON
                        if (mDebugProgress && PathFinderDebug != null)
                            PathFinderDebug(mLocationX, mLocationY, mNewLocationX, mNewLocationY, PathFinderNodeType.Open, mCalcGrid[mNewLocation].F, mCalcGrid[mNewLocation].G);
                        #endif

                        //It is faster if we leave the open node in the priority queue
                        //When it is removed, it will be already closed, it will be ignored automatically
                        //if (tmpGrid[newLocation].Status == 1)
                        //{
                        //    //int removeX   = newLocation & gridXMinus1;
                        //    //int removeY   = newLocation >> gridYLog2;
                        //    mOpen.RemoveLocation(newLocation);
                        //}

                        //if (tmpGrid[newLocation].Status != 1)
                        //{
                            mOpen.Push(mNewLocation);
                        //}
                        mCalcGrid[mNewLocation].Status = mOpenNodeValue;
                    }

                    mCloseNodeCounter++;
                    mCalcGrid[mLocation].Status = mCloseNodeValue;

                    #if DEBUGON
                    if (mDebugProgress && PathFinderDebug != null)
                        PathFinderDebug(0, 0, mLocationX, mLocationY, PathFinderNodeType.Close, mCalcGrid[mLocation].F, mCalcGrid[mLocation].G);
                    #endif
                }

                mCompletedTime = HighResolutionTime.GetTime();
                if (mFound)
                {
                    mClose.Clear();
                    int posX = (int)end.x;
                    int posY = (int)end.y;

                    PathFinderNodeFast fNodeTmp = mCalcGrid[((int)end.y << mGridYLog2) + (int)end.x];
                    PathFinderNode fNode;
                    fNode.F  = fNodeTmp.F;
                    fNode.G  = fNodeTmp.G;
                    fNode.H  = 0;
                    fNode.PX = fNodeTmp.PX;
                    fNode.PY = fNodeTmp.PY;
                    fNode.X  = (int)end.x;
                    fNode.Y  = (int)end.y;

                    while(fNode.X != fNode.PX || fNode.Y != fNode.PY)
                    {
                        mClose.Add(fNode);
                        #if DEBUGON
                        if (mDebugFoundPath && PathFinderDebug != null)
                            PathFinderDebug(fNode.PX, fNode.PY, fNode.X, fNode.Y, PathFinderNodeType.Path, fNode.F, fNode.G);
                        #endif
                        posX = fNode.PX;
                        posY = fNode.PY;
                        fNodeTmp = mCalcGrid[(posY << mGridYLog2) + posX];
                        fNode.F  = fNodeTmp.F;
                        fNode.G  = fNodeTmp.G;
                        fNode.H  = 0;
                        fNode.PX = fNodeTmp.PX;
                        fNode.PY = fNodeTmp.PY;
                        fNode.X  = posX;
                        fNode.Y  = posY;
                    } 

                    mClose.Add(fNode);
                    #if DEBUGON
                    if (mDebugFoundPath && PathFinderDebug != null)
                        PathFinderDebug(fNode.PX, fNode.PY, fNode.X, fNode.Y, PathFinderNodeType.Path, fNode.F, fNode.G);
                    #endif

                    mStopped = true;
                    return mClose;
                }
                mStopped = true;
                return null;
            }
        }
        #endregion

        #region Inner Classes
        [Author("Franco, Gustavo")]
        internal class ComparePFNodeMatrix : IComparer<int>
        {
            #region Variables Declaration
            PathFinderNodeFast[] mMatrix;
            #endregion

            #region Constructors
            public ComparePFNodeMatrix(PathFinderNodeFast[] matrix)
            {
                mMatrix = matrix;
            }
            #endregion

            #region IComparer Members
            public int Compare(int a, int b)
            {
                if (mMatrix[a].F > mMatrix[b].F)
                    return 1;
                else if (mMatrix[a].F < mMatrix[b].F)
                    return -1;
                return 0;
            }
            #endregion
        }
        #endregion
    }
}
