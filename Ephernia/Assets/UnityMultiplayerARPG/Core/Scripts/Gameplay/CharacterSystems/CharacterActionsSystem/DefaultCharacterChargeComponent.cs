﻿using LiteNetLib.Utils;
using UnityEngine;

namespace MultiplayerARPG
{
    public class DefaultCharacterChargeComponent : BaseNetworkedGameEntityComponent<BaseCharacterEntity>, ICharacterChargeComponent
    {
        public bool IsCharging { get; protected set; }
        public bool WillDoActionWhenStopCharging
        {
            get
            {
                return IsCharging && (Time.unscaledTime - chargeStartTime >= chargeDuration);
            }
        }
        public float MoveSpeedRateWhileCharging { get; protected set; }
        public MovementRestriction MovementRestrictionWhileCharging { get; protected set; }

        protected float chargeStartTime;
        protected float chargeDuration;
        protected bool sendingClientStartCharge;
        protected bool sendingClientStopCharge;
        protected bool sendingServerStartCharge;
        protected bool sendingServerStopCharge;
        protected bool sendingIsLeftHand;

        public virtual void ClearChargeStates()
        {
            IsCharging = false;
        }

        protected void PlayChargeAnimation(bool isLeftHand)
        {
            // Get weapon type data
            IWeaponItem weaponItem = Entity.GetAvailableWeapon(ref isLeftHand).GetWeaponItem();
            int weaponTypeDataId = weaponItem.WeaponType.DataId;
            // Play animation
            if (Entity.CharacterModel && Entity.CharacterModel.gameObject.activeSelf)
            {
                // TPS model
                Entity.CharacterModel.PlayWeaponChargeClip(weaponTypeDataId, isLeftHand);
                Entity.CharacterModel.PlayEquippedWeaponCharge(isLeftHand);
            }
            if (Entity.PassengingVehicleModel && Entity.PassengingVehicleModel is BaseCharacterModel vehicleModel)
            {
                // Vehicle model
                vehicleModel.PlayWeaponChargeClip(weaponTypeDataId, isLeftHand);
                vehicleModel.PlayEquippedWeaponCharge(isLeftHand);
            }
            if (IsClient && Entity.FpsModel && Entity.FpsModel.gameObject.activeSelf)
            {
                // FPS model
                Entity.FpsModel.PlayWeaponChargeClip(weaponTypeDataId, isLeftHand);
                Entity.FpsModel.PlayEquippedWeaponCharge(isLeftHand);
            }
            // Set weapon charging state
            MoveSpeedRateWhileCharging = Entity.GetMoveSpeedRateWhileCharging(weaponItem);
            MovementRestrictionWhileCharging = Entity.GetMovementRestrictionWhileCharging(weaponItem);
            IsCharging = true;
            chargeStartTime = Time.unscaledTime;
            chargeDuration = weaponItem.ChargeDuration;
        }

        protected void StopChargeAnimation()
        {
            // Play animation
            if (Entity.CharacterModel && Entity.CharacterModel.gameObject.activeSelf)
            {
                // TPS model
                Entity.CharacterModel.StopWeaponChargeAnimation();
            }
            if (Entity.PassengingVehicleModel && Entity.PassengingVehicleModel is BaseCharacterModel vehicleModel)
            {
                // Vehicle model
                vehicleModel.StopWeaponChargeAnimation();
            }
            if (IsClient && Entity.FpsModel && Entity.FpsModel.gameObject.activeSelf)
            {
                // FPS model
                Entity.FpsModel.StopWeaponChargeAnimation();
            }
            // Set weapon charging state
            IsCharging = false;
        }

        public void StartCharge(bool isLeftHand)
        {
            if (!IsServer && IsOwnerClient)
            {
                // Simulate start charge at client immediately
                PlayChargeAnimation(isLeftHand);
                // Tell the server to start charge
                sendingClientStartCharge = true;
                sendingIsLeftHand = isLeftHand;
            }
            else if (IsOwnerClientOrOwnedByServer)
            {
                ProceedStartChargeStateAtServer(isLeftHand);
            }
        }

        public void StopCharge()
        {
            if (!IsServer && IsOwnerClient)
            {
                // Simulate stop charge at client immediately
                StopChargeAnimation();
                // Tell the server to stop charge
                sendingClientStopCharge = true;
            }
            else if (IsOwnerClientOrOwnedByServer)
            {
                ProceedStopChargeStateAtServer();
            }
        }

        public bool WriteClientStartChargeState(NetDataWriter writer)
        {
            if (sendingClientStartCharge)
            {
                writer.Put(sendingIsLeftHand);
                sendingClientStartCharge = false;
                return true;
            }
            return false;
        }

        public bool WriteServerStartChargeState(NetDataWriter writer)
        {
            if (sendingServerStartCharge)
            {
                writer.Put(sendingIsLeftHand);
                sendingServerStartCharge = false;
                return true;
            }
            return false;
        }

        public bool WriteClientStopChargeState(NetDataWriter writer)
        {
            if (sendingClientStopCharge)
            {
                sendingClientStopCharge = false;
                return true;
            }
            return false;
        }

        public bool WriteServerStopChargeState(NetDataWriter writer)
        {
            if (sendingServerStopCharge)
            {
                sendingServerStopCharge = false;
                return true;
            }
            return false;
        }

        public void ReadClientStartChargeStateAtServer(NetDataReader reader)
        {
            bool isLeftHand = reader.GetBool();
            ProceedStartChargeStateAtServer(isLeftHand);
        }

        protected void ProceedStartChargeStateAtServer(bool isLeftHand)
        {
#if UNITY_EDITOR || UNITY_SERVER
            // Start charge at server immediately
            PlayChargeAnimation(isLeftHand);
            // Tell clients to start charge later
            sendingServerStartCharge = true;
            sendingIsLeftHand = isLeftHand;
#endif
        }

        public void ReadServerStartChargeStateAtClient(NetDataReader reader)
        {
            bool isLeftHand = reader.GetBool();
            if (IsOwnerClientOrOwnedByServer)
            {
                // Don't start charge again (it already played in `StartCharge` function)
                return;
            }
            PlayChargeAnimation(isLeftHand);
        }

        public void ReadClientStopChargeStateAtServer(NetDataReader reader)
        {
            ProceedStopChargeStateAtServer();
        }

        protected void ProceedStopChargeStateAtServer()
        {
#if UNITY_EDITOR || UNITY_SERVER
            // Stop charge at server immediately
            StopChargeAnimation();
            // Tell clients to stop charge later
            sendingServerStopCharge = true;
#endif
        }

        public void ReadServerStopChargeStateAtClient(NetDataReader reader)
        {
            if (IsOwnerClientOrOwnedByServer)
            {
                // Don't stop charge again (it already played in `StopCharge` function)
                return;
            }
            StopChargeAnimation();
        }
    }
}
