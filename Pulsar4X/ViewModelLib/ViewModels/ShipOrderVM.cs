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
    public class ShipOrderVM : IViewModel
    {

        private DictionaryVM<StarSystem, string> _starSystems = new DictionaryVM<StarSystem, string>();
        public DictionaryVM<StarSystem, string> StarSystems 
        { 
            get 
            { 
                return _starSystems; 
            } 
            set 
            { 
                _starSystems = value;
                _starSystems.SelectedIndex = 0;
                RefreshShips(0, 0); 
            } 
        } //these must be properties

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
                RefreshOrders(0,0); 
            } 
        }

        private DictionaryVM<Entity, string> _targetList = new DictionaryVM<Entity, string>();
        public DictionaryVM<Entity, string> TargetList 
        { 
            get 
            { 
                return _targetList; 
            } 
            set 
            {
                _targetList = value;
                _targetList.SelectedIndex = 0;
                OnPropertyChanged(nameof(SelectedTarget));
            } 
        } //not fields!

        private DictionaryVM<BaseOrder, string> _ordersPossible = new DictionaryVM<BaseOrder, string>();
        public DictionaryVM<BaseOrder, string> OrdersPossible 
        { 
            get
            { 
                return _ordersPossible; 
            } 
            set
            { 
                _ordersPossible = value; 
                _ordersPossible.SelectedIndex = 0;
                OnPropertyChanged(nameof(SelectedPossibleOrder));
            }
        }
        private DictionaryVM<BaseOrder, string> _orderList = new DictionaryVM<BaseOrder, string>();
        public DictionaryVM<BaseOrder, string> OrderList 
        {
            get
            { 
                return _orderList;
            }
            set
            { 
                _orderList = value;
                _orderList.SelectedIndex = 0;
                OnPropertyChanged(nameof(SelectedOrder));
            }
        }

        public StarSystem SelectedSystem { get { return _starSystems.SelectedKey; }}
        public Entity SelectedShip { get { return _shipList.SelectedKey; }}
        public BaseOrder SelectedPossibleOrder { get { return _ordersPossible.SelectedKey; } }
        public BaseOrder SelectedOrder { get { return _orderList.SelectedKey; } }
        public Entity SelectedTarget { get { return _targetList.SelectedKey; } }

        public Boolean TargetShown { get; internal set; }
        public int TargetAreaWidth { get; internal set; }

        

        public string ShipSpeed
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return Distance.ToKm(SelectedShip.GetDataBlob<PropulsionDB>().CurrentSpeed.Length()).ToString("N2");
            }
        }

        public string XSpeed
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return Distance.ToKm(SelectedShip.GetDataBlob<PropulsionDB>().CurrentSpeed.X).ToString("N2");
            }
        }

        public string YSpeed
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return Distance.ToKm(SelectedShip.GetDataBlob<PropulsionDB>().CurrentSpeed.Y).ToString("N2");
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

        public string TargetDistance
        {
            get
            {
                if (SelectedShip == null)
                    return "N/A";
                if (SelectedTarget == null)
                    return "N/A";

                Vector4 delta = SelectedShip.GetDataBlob<PositionDB>().AbsolutePosition - SelectedTarget.GetDataBlob<PositionDB>().AbsolutePosition;
                return Distance.ToKm(delta.Length()).ToString("N2") ;
            }
        }

        private GameVM _gameVM;
        public GameVM GameVM { get { return _gameVM; } }

        public ShipOrderVM(GameVM game)
        {
            _gameVM = game;

            FactionInfoDB finfo = _gameVM.CurrentFaction.GetDataBlob<FactionInfoDB>();
            foreach (SystemVM system in _gameVM.StarSystems)
            {
                if(finfo.KnownSystems.Contains(system.StarSystem.Guid))
                {
                    _starSystems.Add(system.StarSystem, system.StarSystem.NameDB.GetName(_gameVM.CurrentFaction));
                }
            }

            _starSystems.SelectedIndex = 0;

            TargetShown = false;
            TargetAreaWidth = 2;

            RefreshShips(0, 0);

            //PropertyChanged += ShipOrderVM_PropertyChanged;
            SelectedSystem.SystemSubpulses.SystemDateChangedEvent += UpdateInterface_SystemDateChangedEvent;

            _starSystems.SelectionChangedEvent += RefreshShips;
            _shipList.SelectionChangedEvent += RefreshOrders;
            _ordersPossible.SelectionChangedEvent += RefreshTarget;
            _targetList.SelectionChangedEvent += RefreshTargetDistance;

            OnPropertyChanged(nameof(StarSystems));
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
            OnPropertyChanged(nameof(TargetDistance));
            RefreshOrderList(0, 0);
        }

        public static ShipOrderVM Create(GameVM game)
        {
            
            return new ShipOrderVM(game);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Refresh(bool partialRefresh = false)
        {
            OnPropertyChanged(nameof(StarSystems));
            RefreshShips(0, 0);

        }

        private void StarSystems_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            RefreshShips(0, 0);
        }

        // Updates the list of ships to give orders to and targets when the system changes
        public void RefreshShips(int a, int b)
        {
            if (SelectedSystem == null)
                return;
            _shipList.Clear();
            foreach(Entity ship in SelectedSystem.SystemManager.GetAllEntitiesWithDataBlob<ShipInfoDB>(_gameVM.CurrentAuthToken))
            {
                if (ship.HasDataBlob<PropulsionDB>())
                    ShipList.Add(ship, ship.GetDataBlob<NameDB>().GetName(_gameVM.CurrentFaction));
            }

            _shipList.SelectedIndex = 0;

            RefreshOrders(0, 0);
            RefreshTarget(0, 0);
            

            OnPropertyChanged(nameof(ShipList));
            OnPropertyChanged(nameof(TargetList));

            OnPropertyChanged(nameof(SelectedShip));
            OnPropertyChanged(nameof(SelectedTarget));

            return;
        }

        public void RefreshTarget(int a, int b)
        {
            int targetIndex;
            if (_targetList.SelectedIndex == -1)
                targetIndex = 0;
            else
                targetIndex = _targetList.SelectedIndex;

            _targetList.Clear();
            foreach (Entity target in SelectedSystem.SystemManager.GetAllEntitiesWithDataBlob<PositionDB>(_gameVM.CurrentAuthToken))
            {
                _targetList.Add(target, target.GetDataBlob<NameDB>().GetName(_gameVM.CurrentFaction));
            }

            _targetList.SelectedIndex = targetIndex;

            if (SelectedPossibleOrder == null)
                TargetShown = false;
            else if (SelectedPossibleOrder.OrderType == orderType.MOVETO)
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
            OnPropertyChanged(nameof(TargetDistance));
        }

        public void RefreshOrders(int a, int b)
        {
            if (SelectedShip == null)
                return;

            _ordersPossible.Clear();

            if (SelectedShip.HasDataBlob<PropulsionDB>())
                _ordersPossible.Add(new MoveOrder(), "Move to");

            _ordersPossible.SelectedIndex = 0;

            RefreshOrderList(0, 0);



            OnPropertyChanged(nameof(SelectedOrder));
            OnPropertyChanged(nameof(SelectedPossibleOrder));

            OnPropertyChanged(nameof(ShipSpeed));
            OnPropertyChanged(nameof(XSpeed));
            OnPropertyChanged(nameof(YSpeed));
            OnPropertyChanged(nameof(XPos));
            OnPropertyChanged(nameof(YPos));


            return;
        }

        public void RefreshOrderList(int a, int b)
        {
            List<BaseOrder> orders = new List<BaseOrder>(SelectedShip.GetDataBlob<ShipInfoDB>().Orders);

            _orderList.Clear();

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
                _orderList.Add(order, orderDescription);
            }

            OnPropertyChanged(nameof(OrderList));
            OnPropertyChanged(nameof(OrdersPossible));
        }

        public void OnAddOrder()
        {
            // Check if Ship, Target, and Order are set
            if (SelectedShip == null  || SelectedTarget == null || SelectedPossibleOrder == null) 
                return;
            switch(SelectedPossibleOrder.OrderType)
            {
                case orderType.MOVETO:
                    _gameVM.CurrentPlayer.Orders.MoveOrder(SelectedShip, SelectedTarget);
                    break;
                case orderType.INVALIDORDER:
                    break;
                default:
                    break;
            }

            _gameVM.CurrentPlayer.ProcessOrders();

            RefreshOrders(0,0);
            
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
                if(nextOrder != SelectedOrder)
                    orderList.Enqueue(nextOrder);
            }

            
            RefreshOrders(0,0);
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
