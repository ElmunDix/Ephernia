﻿using System.Collections.Generic;

namespace MultiplayerARPG.MMO
{
    public partial struct UpdateStorageItemsReq
    {
        public StorageType StorageType { get; set; }
        public string StorageOwnerId { get; set; }
        public List<CharacterItem> StorageItems { get; set; }
        public bool UpdateCharacterData { get; set; }
        public PlayerCharacterData CharacterData { get; set; }
    }
}
