﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;

namespace PuppetMaster.Commands
{
    public interface ICommand
    {
            void Execute(string[] parameters, Dictionary<String, IProcessCreationService> processesPCS);
    }
}