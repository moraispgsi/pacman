﻿using Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    [Serializable]
    public class MonsterHorizontal : IMonster
    {
        public const int WIDTH = 40;
        public const int HEIGHT = 36;
        public const int SPEED = 3;

        public Point Position { get; set; } 
        public Point Speed { get; set; }

        public void step(IStage stage)
        {
            Position = new Point(Position.X + SPEED, Position.Y);
        }
    }
}
