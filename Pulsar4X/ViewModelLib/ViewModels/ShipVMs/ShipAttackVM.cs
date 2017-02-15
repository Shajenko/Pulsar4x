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
    public class ShipAttackVM : IViewModel
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

        private DictionaryVM<Entity, string> _shipList = new DictionaryVM<Entity, string>();
        public DictionaryVM<Entity, string> ShipList
        {
            get
            {
                return _shipList;
            }
            set
            {
                _shipList = value;
                _shipList.SelectedIndex = 0;
            }
        }

        private DictionaryVM<Entity, string> _attackTargetList = new DictionaryVM<Entity, string>();
        public DictionaryVM<Entity, string> AttackTargetList
        {
            get
            {
                return _attackTargetList;
            }
            set
            {
                _attackTargetList = value;
                _attackTargetList.SelectedIndex = 0;
                OnPropertyChanged(nameof(SelectedAttackTarget));
            }
        } //not fields!


        private DictionaryVM<Entity, string> _fireControlList = new DictionaryVM<Entity, string>();
        public DictionaryVM<Entity, string> FireControlList
        {
            get
            {
                return _fireControlList;
            }
            set
            {
                _fireControlList = value;
                _fireControlList.SelectedIndex = 0;
                OnPropertyChanged(nameof(SelectedFireControl));
            }
        }

        private DictionaryVM<Entity, string> _attachedBeamList = new DictionaryVM<Entity, string>();
        public DictionaryVM<Entity, string> AttachedBeamList
        {
            get
            {
                return _attachedBeamList;
            }
            set
            {
                _attachedBeamList = value;
                _attachedBeamList.SelectedIndex = 0;
                OnPropertyChanged(nameof(SelectedAttachedBeam));
            }
        }

        private DictionaryVM<Entity, string> _freeBeamList = new DictionaryVM<Entity, string>();
        public DictionaryVM<Entity, string> FreeBeamList
        {
            get
            {
                return _freeBeamList;
            }
            set
            {
                _freeBeamList = value;
                _freeBeamList.SelectedIndex = 0;
                OnPropertyChanged(nameof(SelectedFreeBeam));
            }
        }

        public StarSystem SelectedSystem { get { return StarSystem; } }
        public Entity SelectedShip { get { return _shipList.SelectedKey; } }
        public Entity SelectedAttackTarget { get { return _attackTargetList.SelectedKey; } }
        public Entity SelectedFireControl { get { return _fireControlList.SelectedKey; } }
        public Entity SelectedAttachedBeam { get { return _attachedBeamList.SelectedKey; } }
        public Entity SelectedFreeBeam { get { return _freeBeamList.SelectedKey; } }

        private Entity _targetedEntity;
        public string TargetedEntity
        {
            get
            {
                if (_targetedEntity == null)
                    return "None";
                else
                    return _targetedEntity.GetDataBlob<NameDB>().DefaultName;
            }
        }

        public Boolean TargetShown { get; internal set; }
        public int TargetAreaWidth { get; internal set; }

                private GameVM _gameVM;
        public GameVM GameVM { get { return _gameVM; } }

        public ShipAttackVM(ShipOrderVM soVM)
        {
            _gameVM = soVM.GameVM;

            FactionInfoDB finfo = _gameVM.CurrentFaction.GetDataBlob<FactionInfoDB>();
            _currentStarSystem = soVM.SelectedSystem;

            TargetShown = false;
            TargetAreaWidth = 2;

            RefreshShips(0, 0);

            _shipList.SelectionChangedEvent += RefreshFireControlList;
            _fireControlList.SelectionChangedEvent += RefreshBeamWeaponsList;
            _fireControlList.SelectionChangedEvent += RefreshFCTarget;

            OnPropertyChanged(nameof(StarSystem));
            OnPropertyChanged(nameof(SelectedSystem));
        }


        public void UpdateInterface_SystemDateChangedEvent(DateTime newDate)
        {
            RefreshFireControlList(0, 0);
        }

        public static ShipAttackVM Create(ShipOrderVM soVM)
        {

            return new ShipAttackVM(soVM);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Refresh(bool partialRefresh = false)
        {
            OnPropertyChanged(nameof(StarSystem));

        }

        // Updates the list of ships to give orders to and targets when the system changes
        public void RefreshShips(int a, int b)
        {
            if (SelectedSystem == null )
                return;
            if (_shipList.Count > 0)
                _shipList.Clear();
            foreach (Entity ship in SelectedSystem.SystemManager.GetAllEntitiesWithDataBlob<ShipInfoDB>(_gameVM.CurrentAuthToken))
            {
                if (ship.HasDataBlob<PropulsionDB>())
                    ShipList.Add(ship, ship.GetDataBlob<NameDB>().GetName(_gameVM.CurrentFaction));
            }

            _shipList.SelectedIndex = 0;

            //RefreshTarget(0, 0);

            return;
        }

        public void RefreshTarget(int a, int b)
        {

            if (StarSystem != null) //if b is not a valid selection
                return;


            _attackTargetList.Clear();

            int attackTargetIndex = _attackTargetList.SelectedIndex;

            foreach (Entity target in SelectedSystem.SystemManager.GetAllEntitiesWithDataBlob<PositionDB>(_gameVM.CurrentAuthToken))
            {
                if (target != SelectedShip)
                {
                    if (target.HasDataBlob<SensorProfileDB>())
                        _attackTargetList.Add(target, target.GetDataBlob<NameDB>().GetName(_gameVM.CurrentFaction));

                }

            }

            _attackTargetList.SelectedIndex = attackTargetIndex;

            if (TargetShown)
                TargetAreaWidth = 200;
            else
                TargetAreaWidth = 2;

            OnPropertyChanged(nameof(TargetShown));
            OnPropertyChanged(nameof(TargetAreaWidth));
        }

        public void RefreshFCTarget(int a, int b)
        {
            if (SelectedFireControl == null || _fireControlList.SelectedIndex == -1)
                return;

            _targetedEntity = SelectedFireControl.GetDataBlob<FireControlInstanceAbilityDB>().Target;
            OnPropertyChanged(TargetedEntity);
        }


        public void RefreshFireControlList(int a, int b)
        {
            _fireControlList.Clear();

            if (SelectedShip == null)
                return;

            if (!SelectedShip.HasDataBlob<BeamWeaponsDB>())
            {
                _fireControlList.Clear();
                return;
            }



            // The component instances all seem to think that their parent entity is Ensuing Calm, regardless of SelectedShip
            var instanceDB = SelectedShip.GetDataBlob<ComponentInstancesDB>();
            List<KeyValuePair<Entity, List<Entity>>> fcList = EntityStoreHelpers.GetComponentsOfType<BeamFireControlAtbDB>(instanceDB.SpecificInstances);
            //new List<KeyValuePair<Entity, List<Entity>>>(instanceDB.SpecificInstances.ToDictionary().Where(item => item.Key.HasDataBlob<BeamFireControlAtbDB>()).ToList());
            foreach (KeyValuePair<Entity, List<Entity>> kvp in fcList)
            {
                int fcCount = 0;
                if (kvp.Key.HasDataBlob<BeamFireControlAtbDB>())
                    foreach (Entity instance in kvp.Value)
                    {
                        fcCount++;
                        _fireControlList.Add(instance, kvp.Key.GetDataBlob<NameDB>().DefaultName + fcCount);
                    }


            }

            _fireControlList.SelectedIndex = 0;



            RefreshBeamWeaponsList(0, 0);

            //            OnPropertyChanged(nameof(FireControlList));

        }

        public void RefreshBeamWeaponsList(int a, int b)
        {
            _attachedBeamList.Clear();
            _freeBeamList.Clear();

            if (SelectedShip == null || _shipList.SelectedIndex == -1)
                return;

            if (_fireControlList.Count > 0 && _fireControlList.SelectedIndex != -1)
            {
                int beamCount = 0;
                foreach (Entity beam in SelectedFireControl.GetDataBlob<FireControlInstanceAbilityDB>().AssignedWeapons)
                {
                    beamCount++;
                    _attachedBeamList.Add(beam, beam.GetDataBlob<ComponentInstanceInfoDB>().DesignEntity.GetDataBlob<NameDB>().DefaultName + " " + beamCount);
                }

            }
            else
                _attachedBeamList.Clear();
            var instancesDB = SelectedShip.GetDataBlob<ComponentInstancesDB>();
            List<KeyValuePair<Entity, List<Entity>>> beamList = EntityStoreHelpers.GetComponentsOfType<BeamWeaponAtbDB>(instancesDB.SpecificInstances);
            beamList.AddRange(EntityStoreHelpers.GetComponentsOfType<SimpleBeamWeaponAtbDB>(instancesDB.SpecificInstances));
            //new List<KeyValuePair<Entity, List<Entity>>>(SelectedShip.GetDataBlob<ComponentInstancesDB>().SpecificInstances.Where(item => item.Key.HasDataBlob<BeamWeaponAtbDB>() || item.Key.HasDataBlob<SimpleBeamWeaponAtbDB>()).ToList());

            bool isBeamControlled = false;
            _freeBeamList.Clear();

            // Get a list of all beam weapons not currently controlled by a fire control
            // @todo: make sure you check all fire controls - currently only lists
            // beams not set to the current fire control
            foreach (KeyValuePair<Entity, List<Entity>> kvp in beamList)
            {
                int beamCount = 0;
                foreach (Entity instance in kvp.Value)
                {
                    if (instance.GetDataBlob<WeaponStateDB>().FireControl == null)
                        _freeBeamList.Add(new KeyValuePair<Entity, string>(instance, kvp.Key.GetDataBlob<NameDB>().DefaultName + " " + ++beamCount));

                }
            }

            OnPropertyChanged(nameof(AttachedBeamList));
            OnPropertyChanged(nameof(FreeBeamList));

        }


        public void OnAddTarget()
        {
            Entity fc = SelectedFireControl;
            Entity target = SelectedAttackTarget;

            if (SelectedFireControl == null || _fireControlList.SelectedIndex == -1)
                return;

            if (SelectedAttackTarget == null || _attackTargetList.SelectedIndex == -1)
                return;

            fc.GetDataBlob<FireControlInstanceAbilityDB>().Target = target;
            // Get the currently selected ship and fire control and the currently selected list of targets
            // Add the currently selected target to the selected ship's target
            // Update GUI

            RefreshFireControlList(0, 0);
        }

        public void OnRemoveTarget()
        {
            // Get the currently selected ship fire control
            // Clear its selected target
            // Update GUI

        }


        private ICommand _addTarget;
        public ICommand AddTarget
        {
            get
            {
                return _addTarget ?? (_addTarget = new CommandHandler(OnAddTarget, true));
            }
        }

        private ICommand _removeTarget;
        public ICommand RemoveTarget
        {
            get
            {
                return _removeTarget ?? (_removeTarget = new CommandHandler(OnRemoveTarget, true));
            }
        }



    }
}
