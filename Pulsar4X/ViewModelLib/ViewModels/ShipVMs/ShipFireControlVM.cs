using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Pulsar4X.ViewModel
{
    public class ShipFireControlVM : IViewModel
    {
        private StarSystem _currentStarSystem;
        public StarSystem StarSystem
        {
            get
            {
                return _currentStarSystem;
            }
            set
            {
                _currentStarSystem = value;
                RefreshShips(0, 0);
            }
        }
    }
}
