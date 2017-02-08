﻿using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Pulsar4X.ViewModel
{
    public class ShipMoveVM : IViewModel
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
                RefreshOrders(0, 0);
            }
        }

        private DictionaryVM<Entity, string> _moveTargetList = new DictionaryVM<Entity, string>();
        public DictionaryVM<Entity, string> MoveTargetList
        {
            get
            {
                return _moveTargetList;
            }
            set
            {
                _moveTargetList = value;
                _moveTargetList.SelectedIndex = 0;
                OnPropertyChanged(nameof(SelectedMoveTarget));
            }
        }

        private DictionaryVM<BaseOrder, string> _moveOrdersPossible = new DictionaryVM<BaseOrder, string>();
        public DictionaryVM<BaseOrder, string> MoveOrdersPossible
        {
            get
            {
                return _moveOrdersPossible;
            }
            set
            {
                _moveOrdersPossible = value;
                _moveOrdersPossible.SelectedIndex = 0;
                OnPropertyChanged(nameof(SelectedPossibleMoveOrder));
            }
        }

        private DictionaryVM<BaseOrder, string> _moveOrderList = new DictionaryVM<BaseOrder, string>();
        public DictionaryVM<BaseOrder, string> MoveOrderList
        {
            get
            {
                return _moveOrderList;
            }
            set
            {
                _moveOrderList = value;
                _moveOrderList.SelectedIndex = 0;
                OnPropertyChanged(nameof(SelectedMoveOrder));
            }
        }

        public StarSystem SelectedSystem { get { return _currentStarSystem; } }
        public Entity SelectedShip { get { return _shipList.SelectedKey; } }
        public BaseOrder SelectedPossibleMoveOrder { get { return _moveOrdersPossible.SelectedKey; } }
        public BaseOrder SelectedMoveOrder { get { return _moveOrderList.SelectedKey; } }
        public Entity SelectedMoveTarget { get { return _moveTargetList.SelectedKey; } }

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



        public string ShipSpeed
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return Distance.AuToKm(SelectedShip.GetDataBlob<PropulsionDB>().CurrentSpeed.Length()).ToString("N2");
            }
        }

        public string XSpeed
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return Distance.AuToKm(SelectedShip.GetDataBlob<PropulsionDB>().CurrentSpeed.X).ToString("N2");
            }
        }

        public string YSpeed
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return Distance.AuToKm(SelectedShip.GetDataBlob<PropulsionDB>().CurrentSpeed.Y).ToString("N2");
            }
        }

        public string XPos
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return SelectedShip.GetDataBlob<PositionDB>().X.ToString("N5");
            }
        }

        public string YPos
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return SelectedShip.GetDataBlob<PositionDB>().Y.ToString("N5");
            }
        }

        public string MaxSpeed
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return SelectedShip.GetDataBlob<PropulsionDB>().MaximumSpeed.ToString("N5");
            }
        }

        public string MoveTargetDistance
        {
            get
            {
                if (SelectedShip == null)
                    return "N/A";
                if (SelectedMoveTarget == null)
                    return "N/A";

                Vector4 delta = SelectedShip.GetDataBlob<PositionDB>().AbsolutePosition - SelectedMoveTarget.GetDataBlob<PositionDB>().AbsolutePosition;
                return Distance.AuToKm(delta.Length()).ToString("N2");
            }
        }

        private GameVM _gameVM;
        public GameVM GameVM { get { return _gameVM; } }

        public ShipMoveVM(ShipOrderVM soVM)
        {
            _gameVM = soVM.GameVM;

            _currentStarSystem = soVM.SelectedSystem;

            TargetShown = false;
            TargetAreaWidth = 2;

            RefreshShips(0, 0);

            soVM.StarSystems.SelectionChangedEvent += RefreshShips;
            _shipList.SelectionChangedEvent += RefreshOrders;
            _moveOrdersPossible.SelectionChangedEvent += RefreshTarget;
            _moveTargetList.SelectionChangedEvent += RefreshTargetDistance;

            OnPropertyChanged(nameof(SelectedSystem));
        }

        // Not 100% on events, but hopefully this will do
        public void UpdateInterface_SystemDateChangedEvent(DateTime newDate)
        {
            OnPropertyChanged(nameof(ShipSpeed));
            OnPropertyChanged(nameof(XSpeed));
            OnPropertyChanged(nameof(YSpeed));
            OnPropertyChanged(nameof(XPos));
            OnPropertyChanged(nameof(YPos));
            OnPropertyChanged(nameof(MaxSpeed));
            OnPropertyChanged(nameof(MoveTargetDistance));
            RefreshOrderList(0, 0);
        }

        public static ShipMoveVM Create(ShipOrderVM soVM)
        {

            return new ShipMoveVM(soVM);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Refresh(bool partialRefresh = false)
        {
            OnPropertyChanged(nameof(SelectedSystem));

        }

        // Updates the list of ships to give orders to and targets when the system changes
        public void RefreshShips(int a, int b)
        {
            if (SelectedSystem == null)
                return;

            _shipList.Clear();
            foreach (Entity ship in SelectedSystem.SystemManager.GetAllEntitiesWithDataBlob<ShipInfoDB>(_gameVM.CurrentAuthToken))
            {
                if (ship.HasDataBlob<PropulsionDB>())
                    ShipList.Add(ship, ship.GetDataBlob<NameDB>().GetName(_gameVM.CurrentFaction));
            }

            _shipList.SelectedIndex = 0;

            //RefreshTarget(0, 0);

            OnPropertyChanged(nameof(ShipList));
            OnPropertyChanged(nameof(MoveTargetList));

            OnPropertyChanged(nameof(SelectedShip));
            OnPropertyChanged(nameof(SelectedMoveTarget));

            return;
        }

        public void RefreshTarget(int a, int b)
        {

            if (_currentStarSystem == null) //if b is not a valid selection
                return;

            _moveTargetList.Clear();

            int moveTargetIndex = _moveTargetList.SelectedIndex;

            foreach (Entity target in SelectedSystem.SystemManager.GetAllEntitiesWithDataBlob<PositionDB>(_gameVM.CurrentAuthToken))
            {
                if (target != SelectedShip)
                {
                    _moveTargetList.Add(target, target.GetDataBlob<NameDB>().GetName(_gameVM.CurrentFaction));
                }

            }

            _moveTargetList.SelectedIndex = moveTargetIndex;

            if (SelectedPossibleMoveOrder == null)
                TargetShown = false;
            else if (SelectedPossibleMoveOrder.OrderType == orderType.MOVETO)
                TargetShown = true;
            else
                TargetShown = false;

            if (TargetShown)
                TargetAreaWidth = 200;
            else
                TargetAreaWidth = 2;

            OnPropertyChanged(nameof(TargetShown));
            OnPropertyChanged(nameof(TargetAreaWidth));
        }

        public void RefreshTargetDistance(int a, int b)
        {
            OnPropertyChanged(nameof(MoveTargetDistance));
        }

        public void RefreshOrders(int a, int b)
        {
            if (SelectedShip == null)
                return;

            _moveOrdersPossible.Clear();

            if (SelectedShip.HasDataBlob<PropulsionDB>())
                _moveOrdersPossible.Add(new MoveOrder(), "Move to");

            _moveOrdersPossible.SelectedIndex = 0;

            RefreshOrderList(0, 0);

            OnPropertyChanged(nameof(SelectedMoveOrder));
            OnPropertyChanged(nameof(SelectedPossibleMoveOrder));

            OnPropertyChanged(nameof(ShipSpeed));
            OnPropertyChanged(nameof(XSpeed));
            OnPropertyChanged(nameof(YSpeed));
            OnPropertyChanged(nameof(XPos));
            OnPropertyChanged(nameof(YPos));
            OnPropertyChanged(nameof(MaxSpeed));

            return;
        }

        public void RefreshOrderList(int a, int b)
        {
            if (SelectedShip == null)
                return;
            List<BaseOrder> orders = new List<BaseOrder>(SelectedShip.GetDataBlob<ShipInfoDB>().Orders);

            _moveOrderList.Clear();

            foreach (BaseOrder order in orders)
            {
                string orderDescription = "";

                switch (order.OrderType)
                {
                    case orderType.MOVETO:
                        MoveOrder moveOrder = (MoveOrder)order;
                        orderDescription += "Move to ";
                        orderDescription += moveOrder.Target.GetDataBlob<NameDB>().GetName(_gameVM.CurrentFaction);
                        break;
                    default:
                        break;
                }
                _moveOrderList.Add(order, orderDescription);
            }

            OnPropertyChanged(nameof(MoveOrderList));
            OnPropertyChanged(nameof(MoveOrdersPossible));
        }

        public void OnAddOrder()
        {
            // Check if Ship, Target, and Order are set
            if (SelectedShip == null || SelectedMoveTarget == null || SelectedPossibleMoveOrder == null)
                return;
            switch (SelectedPossibleMoveOrder.OrderType)
            {
                case orderType.MOVETO:
                    _gameVM.CurrentPlayer.Orders.MoveOrder(SelectedShip, SelectedMoveTarget);
                    break;
                case orderType.INVALIDORDER:
                    break;
                default:
                    break;
            }

            _gameVM.CurrentPlayer.ProcessOrders();

            RefreshOrders(0, 0);

        }

        public void OnRemoveOrder()
        {


            if (SelectedShip == null)
                return;

            BaseOrder nextOrder;
            Queue<BaseOrder> orderList = SelectedShip.GetDataBlob<ShipInfoDB>().Orders;


            int totalOrders = orderList.Count;

            for (int i = 0; i < totalOrders; i++)
            {
                nextOrder = orderList.Dequeue();
                if (nextOrder != SelectedMoveOrder)
                    orderList.Enqueue(nextOrder);
            }


            RefreshOrders(0, 0);
        }

        private ICommand _addOrder;
        public ICommand AddOrder
        {
            get
            {
                return _addOrder ?? (_addOrder = new CommandHandler(OnAddOrder, true));
            }
        }

        private ICommand _removeOrder;
        public ICommand RemoveOrder
        {
            get
            {
                return _removeOrder ?? (_removeOrder = new CommandHandler(OnRemoveOrder, true));
            }
        }


    }
}
