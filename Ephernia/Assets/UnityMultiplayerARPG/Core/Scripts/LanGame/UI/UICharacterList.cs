﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MultiplayerARPG
{
    public class UICharacterList : UIBase
    {
        public UICharacter uiCharacterPrefab;
        public Transform uiCharacterContainer;
        public Transform characterModelContainer;
        [Header("UI Elements")]
        public Button buttonStart;
        public Button buttonDelete;
        [Tooltip("These objects will be activated while character selected")]
        public List<GameObject> selectedCharacterObjects = new List<GameObject>();
        [Header("Event")]
        public UnityEvent eventOnNoCharacter = new UnityEvent();
        public UnityEvent eventOnAbleToCreateCharacter = new UnityEvent();
        public UnityEvent eventOnNotAbleToCreateCharacter = new UnityEvent();

        private UIList cacheCharacterList;
        public UIList CacheCharacterList
        {
            get
            {
                if (cacheCharacterList == null)
                {
                    cacheCharacterList = gameObject.AddComponent<UIList>();
                    cacheCharacterList.uiPrefab = uiCharacterPrefab.gameObject;
                    cacheCharacterList.uiContainer = uiCharacterContainer;
                }
                return cacheCharacterList;
            }
        }

        private UICharacterSelectionManager cacheCharacterSelectionManager;
        public UICharacterSelectionManager CacheCharacterSelectionManager
        {
            get
            {
                if (cacheCharacterSelectionManager == null)
                    cacheCharacterSelectionManager = gameObject.GetOrAddComponent<UICharacterSelectionManager>();
                cacheCharacterSelectionManager.selectionMode = UISelectionMode.Toggle;
                return cacheCharacterSelectionManager;
            }
        }

        protected readonly Dictionary<string, BaseCharacterModel> CharacterModelById = new Dictionary<string, BaseCharacterModel>();
        protected BaseCharacterModel selectedModel;
        public BaseCharacterModel SelectedModel { get { return selectedModel; } }
        protected readonly Dictionary<string, PlayerCharacterData> PlayerCharacterDataById = new Dictionary<string, PlayerCharacterData>();
        protected PlayerCharacterData selectedPlayerCharacterData;
        public PlayerCharacterData SelectedPlayerCharacterData { get { return selectedPlayerCharacterData; } }

        protected virtual void LoadCharacters()
        {
            CacheCharacterSelectionManager.Clear();
            CacheCharacterList.HideAll();
            // Unenable buttons
            if (buttonStart)
                buttonStart.gameObject.SetActive(false);
            if (buttonDelete)
                buttonDelete.gameObject.SetActive(false);
            // Deactivate selected character objects
            foreach (GameObject obj in selectedCharacterObjects)
            {
                obj.SetActive(false);
            }
            // Remove all models
            characterModelContainer.RemoveChildren();
            CharacterModelById.Clear();
            // Remove all cached data
            PlayerCharacterDataById.Clear();
            // Show list of created characters
            List<PlayerCharacterData> selectableCharacters = GameInstance.Singleton.SaveSystem.LoadCharacters();
            for (int i = selectableCharacters.Count - 1; i >= 0; --i)
            {
                PlayerCharacterData selectableCharacter = selectableCharacters[i];
                if (selectableCharacter == null ||
                    !GameInstance.PlayerCharacterEntities.ContainsKey(selectableCharacter.EntityId) ||
                    !GameInstance.PlayerCharacters.ContainsKey(selectableCharacter.DataId))
                {
                    // If invalid entity id or data id, remove from selectable character list
                    selectableCharacters.RemoveAt(i);
                }
            }

            if (GameInstance.Singleton.maxCharacterSaves > 0 &&
                selectableCharacters.Count >= GameInstance.Singleton.maxCharacterSaves)
                eventOnNotAbleToCreateCharacter.Invoke();
            else
                eventOnAbleToCreateCharacter.Invoke();

            // Clear selected character data, will select first in list if available
            (BaseGameNetworkManager.Singleton as LanRpgNetworkManager).selectedCharacter = selectedPlayerCharacterData = null;

            // Generate list entry by saved characters
            if (selectableCharacters.Count > 0)
            {
                selectableCharacters.Sort(new PlayerCharacterDataLastUpdateComparer().Desc());
                CacheCharacterList.Generate(selectableCharacters, (index, characterData, ui) =>
                {
                    // Cache player character to dictionary, we will use it later
                    PlayerCharacterDataById[characterData.Id] = characterData;
                    // Setup UIs
                    UICharacter uiCharacter = ui.GetComponent<UICharacter>();
                    uiCharacter.NotForOwningCharacter = true;
                    uiCharacter.Data = characterData;
                    // Select trigger when add first entry so deactivate all models is okay beacause first model will active
                    BaseCharacterModel characterModel = characterData.InstantiateModel(characterModelContainer);
                    if (characterModel != null)
                    {
                        CharacterModelById[characterData.Id] = characterModel;
                        characterModel.SetEquipWeapons(characterData.SelectableWeaponSets, characterData.EquipWeaponSet, false);
                        characterModel.SetEquipItems(characterData.EquipItems);
                        characterModel.gameObject.SetActive(false);
                        CacheCharacterSelectionManager.Add(uiCharacter);
                    }
                });
            }
            else
            {
                eventOnNoCharacter.Invoke();
            }
        }

        public virtual void OnEnable()
        {
            if (buttonStart)
            {
                buttonStart.onClick.RemoveListener(OnClickStart);
                buttonStart.onClick.AddListener(OnClickStart);
                buttonStart.gameObject.SetActive(false);
            }
            if (buttonDelete)
            {
                buttonDelete.onClick.RemoveListener(OnClickDelete);
                buttonDelete.onClick.AddListener(OnClickDelete);
                buttonDelete.gameObject.SetActive(false);
            }
            foreach (GameObject obj in selectedCharacterObjects)
            {
                obj.SetActive(false);
            }
            // Clear selection
            CacheCharacterSelectionManager.eventOnSelect.RemoveListener(OnSelectCharacter);
            CacheCharacterSelectionManager.eventOnSelect.AddListener(OnSelectCharacter);
            CacheCharacterSelectionManager.Clear();
            CacheCharacterList.HideAll();
            // Load characters
            LoadCharacters();
        }

        public virtual void OnDisable()
        {
            characterModelContainer.RemoveChildren();
        }

        protected void OnSelectCharacter(UICharacter uiCharacter)
        {
            selectedPlayerCharacterData = uiCharacter.Data as PlayerCharacterData;
            // Enable buttons because character was selected
            if (buttonStart)
                buttonStart.gameObject.SetActive(true);
            if (buttonDelete)
                buttonDelete.gameObject.SetActive(true);
            // Activate selected character objects because character was selected
            foreach (GameObject obj in selectedCharacterObjects)
            {
                obj.SetActive(true);
            }
            characterModelContainer.SetChildrenActive(false);
            // Show selected character model
            CharacterModelById.TryGetValue(selectedPlayerCharacterData.Id, out selectedModel);
            if (SelectedModel != null)
                SelectedModel.gameObject.SetActive(true);
            OnSelectCharacter(uiCharacter.Data as IPlayerCharacterData);
        }

        protected virtual void OnSelectCharacter(IPlayerCharacterData playerCharacterData)
        {
            // Validate map data
            if (!GameInstance.Singleton.GetGameMapIds().Contains(SelectedPlayerCharacterData.CurrentMapName))
            {
                PlayerCharacter database = SelectedPlayerCharacterData.GetDatabase() as PlayerCharacter;
                BaseMapInfo startMap;
                Vector3 startPosition;
                Vector3 startRotation;
                database.GetStartMapAndTransform(SelectedPlayerCharacterData, out startMap, out startPosition, out startRotation);
                SelectedPlayerCharacterData.CurrentMapName = startMap.Id;
                SelectedPlayerCharacterData.CurrentPosition = startPosition;
                SelectedPlayerCharacterData.CurrentRotation = startRotation;
            }
            // Set selected character to network manager
            (BaseGameNetworkManager.Singleton as LanRpgNetworkManager).selectedCharacter = SelectedPlayerCharacterData;
        }

        public virtual void OnClickStart()
        {
            if (SelectedPlayerCharacterData == null)
            {
                UISceneGlobal.Singleton.ShowMessageDialog(LanguageManager.GetText(UITextKeys.UI_LABEL_ERROR.ToString()), LanguageManager.GetText(UITextKeys.UI_ERROR_NO_CHOSEN_CHARACTER_TO_START.ToString()));
                Debug.LogWarning("Cannot start game, No chosen character");
                return;
            }
            (BaseGameNetworkManager.Singleton as LanRpgNetworkManager).StartGame();
        }

        public virtual void OnClickDelete()
        {
            if (SelectedPlayerCharacterData == null)
            {
                UISceneGlobal.Singleton.ShowMessageDialog(LanguageManager.GetText(UITextKeys.UI_LABEL_ERROR.ToString()), LanguageManager.GetText(UITextKeys.UI_ERROR_NO_CHOSEN_CHARACTER_TO_DELETE.ToString()));
                Debug.LogWarning("Cannot delete character, No chosen character");
                return;
            }
            SelectedPlayerCharacterData.DeletePersistentCharacterData();
            // Reload characters
            LoadCharacters();
        }
    }
}
