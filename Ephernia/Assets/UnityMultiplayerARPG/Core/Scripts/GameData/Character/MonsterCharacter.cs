﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MultiplayerARPG
{
    public enum MonsterCharacteristic
    {
        Normal,
        Aggressive,
        Assist,
        NoHarm,
    }

    [System.Serializable]
    public struct MonsterCharacterAmount
    {
        public MonsterCharacter monster;
        public int amount;
    }

    [CreateAssetMenu(fileName = GameDataMenuConsts.MONSTER_CHARACTER_FILE, menuName = GameDataMenuConsts.MONSTER_CHARACTER_MENU, order = GameDataMenuConsts.MONSTER_CHARACTER_ORDER)]
    public partial class MonsterCharacter : BaseCharacter
    {
        [Category(2, "Monster Settings")]
        [Header("Monster Data")]
        [SerializeField]
        [Tooltip("This will be used to adjust stats. If this value is 100, it means current stats which set to this character data is stats for character level 100, it will be used to adjust stats for character level 1.")]
        private int defaultLevel = 1;
        public int DefaultLevel { get { return defaultLevel; } }
        [SerializeField]
        [Tooltip("`Normal` will attack when being attacked, `Aggressive` will attack when enemy nearby, `Assist` will attack when other with same `Ally Id` being attacked, `NoHarm` won't attack.")]
        private MonsterCharacteristic characteristic = MonsterCharacteristic.Normal;
        public MonsterCharacteristic Characteristic { get { return characteristic; } }
        [SerializeField]
        [Tooltip("This will work with assist characteristic only, to detect ally")]
        private ushort allyId = 0;
        public ushort AllyId { get { return allyId; } }
        [SerializeField]
        [Tooltip("This move speed will be applies when it's wandering. if it's going to chase enemy, stats'moveSpeed will be applies")]
        private float wanderMoveSpeed = 1f;
        public float WanderMoveSpeed { get { return wanderMoveSpeed; } }
        [SerializeField]
        [Tooltip("Range to see an enemies and allies")]
        private float visualRange = 5f;
        public float VisualRange { get { return visualRange; } }
        [SerializeField]
        [Tooltip("Range to see an enemies and allies while summoned")]
        private float summonedVisualRange = 10f;
        public float SummonedVisualRange { get { return summonedVisualRange; } }

        [Category(3, "Character Stats")]
        [SerializeField]
        [FormerlySerializedAs("monsterSkills")]
        private MonsterSkill[] skills = new MonsterSkill[0];
        [SerializeField]
        private Buff summonerBuff = Buff.Empty;
        public Buff SummonerBuff { get { return summonerBuff; } }

        [Category(4, "Attacking")]
        [SerializeField]
        private DamageInfo damageInfo = default(DamageInfo);
        public DamageInfo DamageInfo { get { return damageInfo; } }
        [SerializeField]
        private DamageIncremental damageAmount = default(DamageIncremental);
        public DamageIncremental DamageAmount
        {
            get
            {
                // Adjust base stats by default level
                if (defaultLevel <= 1)
                {
                    return damageAmount;
                }
                else
                {
                    if (!adjustDamageAmount.HasValue)
                    {
                        adjustDamageAmount = new DamageIncremental()
                        {
                            damageElement = damageAmount.damageElement,
                            amount = new IncrementalMinMaxFloat()
                            {
                                baseAmount = damageAmount.amount.baseAmount + (damageAmount.amount.amountIncreaseEachLevel * -(defaultLevel - 1)),
                                amountIncreaseEachLevel = damageAmount.amount.amountIncreaseEachLevel,
                            }
                        };
                    }
                    return adjustDamageAmount.Value;
                }
            }
        }
        [SerializeField]
        private float moveSpeedRateWhileAttacking = 0f;
        public float MoveSpeedRateWhileAttacking { get { return moveSpeedRateWhileAttacking; } }

        [Category(5, "Killing Rewards")]
        [SerializeField]
        private IncrementalMinMaxInt randomExp = default(IncrementalMinMaxInt);
        [SerializeField]
        private IncrementalMinMaxInt randomGold = default(IncrementalMinMaxInt);
        [SerializeField]
        [ArrayElementTitle("currency")]
        public CurrencyRandomAmount[] randomCurrencies = new CurrencyRandomAmount[0];
        [SerializeField]
        [ArrayElementTitle("item")]
        private ItemDrop[] randomItems = new ItemDrop[0];
        [SerializeField]
        private ItemDropTable[] itemDropTables = new ItemDropTable[0];
        [SerializeField]
        private ItemRandomByWeightTable[] itemRandomByWeightTables = new ItemRandomByWeightTable[0];
        [SerializeField]
        [Tooltip("Max kind of items that will be dropped in ground")]
        private byte maxDropItems = 5;

        #region Being deprecated
        [HideInInspector]
        [SerializeField]
        private int randomExpMin;
        [HideInInspector]
        [SerializeField]
        private int randomExpMax;
        [HideInInspector]
        [SerializeField]
        private int randomGoldMin;
        [HideInInspector]
        [SerializeField]
        private int randomGoldMax;
        [HideInInspector]
        [SerializeField]
        private ItemDropTable itemDropTable = null;
        #endregion

        [System.NonSerialized]
        private CharacterStatsIncremental? adjustStats = null;
        [System.NonSerialized]
        private AttributeIncremental[] adjustAttributes = null;
        [System.NonSerialized]
        private ResistanceIncremental[] adjustResistances = null;
        [System.NonSerialized]
        private ArmorIncremental[] adjustArmors = null;
        [System.NonSerialized]
        private DamageIncremental? adjustDamageAmount = null;
        [System.NonSerialized]
        private IncrementalMinMaxInt? adjustRandomExp = null;
        [System.NonSerialized]
        private IncrementalMinMaxInt? adjustRandomGold = null;

        [System.NonSerialized]
        private List<ItemRandomByWeightTable> cacheItemRandomByWeightTables = null;
        public List<ItemRandomByWeightTable> CacheItemRandomByWeightTables
        {
            get
            {
                if (cacheItemRandomByWeightTables == null)
                {
                    cacheItemRandomByWeightTables = new List<ItemRandomByWeightTable>();
                    if (itemRandomByWeightTables != null)
                        cacheItemRandomByWeightTables.AddRange(itemRandomByWeightTables);
                }
                return cacheItemRandomByWeightTables;
            }
        }

        [System.NonSerialized]
        private List<ItemDrop> certainDropItems = new List<ItemDrop>();
        [System.NonSerialized]
        private List<ItemDrop> uncertainDropItems = new List<ItemDrop>();

        [System.NonSerialized]
        private List<ItemDrop> cacheRandomItems = null;
        public List<ItemDrop> CacheRandomItems
        {
            get
            {
                if (cacheRandomItems == null)
                {
                    int i;
                    cacheRandomItems = new List<ItemDrop>();
                    if (randomItems != null &&
                        randomItems.Length > 0)
                    {
                        for (i = 0; i < randomItems.Length; ++i)
                        {
                            if (randomItems[i].item == null ||
                                randomItems[i].maxAmount <= 0 ||
                                randomItems[i].dropRate <= 0)
                                continue;
                            cacheRandomItems.Add(randomItems[i]);
                        }
                    }
                    if (itemDropTables != null &&
                        itemDropTables.Length > 0)
                    {
                        foreach (ItemDropTable itemDropTable in itemDropTables)
                        {
                            if (itemDropTable != null &&
                                itemDropTable.randomItems != null &&
                                itemDropTable.randomItems.Length > 0)
                            {
                                for (i = 0; i < itemDropTable.randomItems.Length; ++i)
                                {
                                    if (itemDropTable.randomItems[i].item == null ||
                                        itemDropTable.randomItems[i].maxAmount <= 0 ||
                                        itemDropTable.randomItems[i].dropRate <= 0)
                                        continue;
                                    cacheRandomItems.Add(itemDropTable.randomItems[i]);
                                }
                            }
                        }
                    }
                    cacheRandomItems.Sort((a, b) => b.dropRate.CompareTo(a.dropRate));
                    certainDropItems.Clear();
                    uncertainDropItems.Clear();
                    for (i = 0; i < cacheRandomItems.Count; ++i)
                    {
                        if (cacheRandomItems[i].dropRate >= 1f)
                            certainDropItems.Add(cacheRandomItems[i]);
                        else
                            uncertainDropItems.Add(cacheRandomItems[i]);
                    }
                }
                return cacheRandomItems;
            }
        }

        [System.NonSerialized]
        private List<CurrencyRandomAmount> cacheRandomCurrencies = null;
        public List<CurrencyRandomAmount> CacheRandomCurrencies
        {
            get
            {
                if (cacheRandomCurrencies == null)
                {
                    int i;
                    cacheRandomCurrencies = new List<CurrencyRandomAmount>();
                    if (randomCurrencies != null &&
                        randomCurrencies.Length > 0)
                    {
                        for (i = 0; i < randomCurrencies.Length; ++i)
                        {
                            if (randomCurrencies[i].currency == null ||
                                randomCurrencies[i].maxAmount <= 0)
                                continue;
                            cacheRandomCurrencies.Add(randomCurrencies[i]);
                        }
                    }
                    if (itemDropTables != null &&
                        itemDropTables.Length > 0)
                    {
                        foreach (ItemDropTable itemDropTable in itemDropTables)
                        {
                            if (itemDropTable != null &&
                                itemDropTable.randomCurrencies != null &&
                                itemDropTable.randomCurrencies.Length > 0)
                            {
                                for (i = 0; i < itemDropTable.randomCurrencies.Length; ++i)
                                {
                                    if (itemDropTable.randomCurrencies[i].currency == null ||
                                        itemDropTable.randomCurrencies[i].maxAmount <= 0)
                                        continue;
                                    cacheRandomCurrencies.Add(itemDropTable.randomCurrencies[i]);
                                }
                            }
                        }
                    }
                }
                return cacheRandomCurrencies;
            }
        }

        public override sealed CharacterStatsIncremental Stats
        {
            get
            {
                // Adjust base stats by default level
                if (defaultLevel <= 1)
                {
                    return base.Stats;
                }
                else
                {
                    if (!adjustStats.HasValue)
                    {
                        adjustStats = new CharacterStatsIncremental()
                        {
                            baseStats = base.Stats.baseStats + (base.Stats.statsIncreaseEachLevel * -(defaultLevel - 1)),
                            statsIncreaseEachLevel = base.Stats.statsIncreaseEachLevel,
                        };
                    }
                    return adjustStats.Value;
                }
            }
        }

        public override sealed AttributeIncremental[] Attributes
        {
            get
            {
                // Adjust base attributes by default level
                if (defaultLevel <= 1)
                {
                    return base.Attributes;
                }
                else
                {
                    if (adjustAttributes == null)
                    {
                        adjustAttributes = new AttributeIncremental[base.Attributes.Length];
                        AttributeIncremental tempValue;
                        for (int i = 0; i < base.Attributes.Length; ++i)
                        {
                            tempValue = base.Attributes[i];
                            adjustAttributes[i] = new AttributeIncremental()
                            {
                                attribute = tempValue.attribute,
                                amount = new IncrementalFloat()
                                {
                                    baseAmount = tempValue.amount.baseAmount + (tempValue.amount.amountIncreaseEachLevel * -(defaultLevel - 1)),
                                    amountIncreaseEachLevel = tempValue.amount.amountIncreaseEachLevel,
                                }
                            };
                        }
                    }
                    return adjustAttributes;
                }
            }
        }

        public override sealed ResistanceIncremental[] Resistances
        {
            get
            {
                // Adjust base resistances by default level
                if (defaultLevel <= 1)
                {
                    return base.Resistances;
                }
                else
                {
                    if (adjustResistances == null)
                    {
                        adjustResistances = new ResistanceIncremental[base.Resistances.Length];
                        ResistanceIncremental tempValue;
                        for (int i = 0; i < base.Resistances.Length; ++i)
                        {
                            tempValue = base.Resistances[i];
                            adjustResistances[i] = new ResistanceIncremental()
                            {
                                damageElement = tempValue.damageElement,
                                amount = new IncrementalFloat()
                                {
                                    baseAmount = tempValue.amount.baseAmount + (tempValue.amount.amountIncreaseEachLevel * -(defaultLevel - 1)),
                                    amountIncreaseEachLevel = tempValue.amount.amountIncreaseEachLevel,
                                }
                            };
                        }
                    }
                    return adjustResistances;
                }
            }
        }

        public override sealed ArmorIncremental[] Armors
        {
            get
            {
                // Adjust base armors by default level
                if (defaultLevel <= 1)
                {
                    return base.Armors;
                }
                else
                {
                    if (adjustArmors == null)
                    {
                        adjustArmors = new ArmorIncremental[base.Armors.Length];
                        ArmorIncremental tempValue;
                        for (int i = 0; i < base.Armors.Length; ++i)
                        {
                            tempValue = base.Armors[i];
                            adjustArmors[i] = new ArmorIncremental()
                            {
                                damageElement = tempValue.damageElement,
                                amount = new IncrementalFloat()
                                {
                                    baseAmount = tempValue.amount.baseAmount + (tempValue.amount.amountIncreaseEachLevel * -(defaultLevel - 1)),
                                    amountIncreaseEachLevel = tempValue.amount.amountIncreaseEachLevel,
                                }
                            };
                        }
                    }
                    return adjustArmors;
                }
            }
        }

        [System.NonSerialized]
        private Dictionary<BaseSkill, int> cacheSkillLevels = null;
        public override Dictionary<BaseSkill, int> CacheSkillLevels
        {
            get
            {
                if (cacheSkillLevels == null)
                    cacheSkillLevels = GameDataHelpers.CombineSkills(skills, new Dictionary<BaseSkill, int>());
                return cacheSkillLevels;
            }
        }

        public IncrementalMinMaxInt AdjustedRandomExp
        {
            get
            {
                // Adjust base stats by default level
                if (defaultLevel <= 1)
                {
                    return randomExp;
                }
                else
                {
                    if (!adjustRandomExp.HasValue)
                    {
                        MinMaxFloat adjustBaseAmount = new MinMaxFloat()
                        {
                            min = randomExp.baseAmount.min,
                            max = randomExp.baseAmount.max,
                        };
                        adjustBaseAmount += randomExp.amountIncreaseEachLevel * -(defaultLevel - 1);
                        adjustRandomExp = new IncrementalMinMaxInt()
                        {
                            baseAmount = new MinMaxInt()
                            {
                                min = (int)adjustBaseAmount.min,
                                max = (int)adjustBaseAmount.max,
                            },
                            amountIncreaseEachLevel = randomExp.amountIncreaseEachLevel,
                        };
                    }
                    return adjustRandomExp.Value;
                }
            }
        }

        public IncrementalMinMaxInt AdjustedRandomGold
        {
            get
            {
                // Adjust base stats by default level
                if (defaultLevel <= 1)
                {
                    return randomGold;
                }
                else
                {
                    if (!adjustRandomGold.HasValue)
                    {
                        MinMaxFloat adjustBaseAmount = new MinMaxFloat()
                        {
                            min = randomExp.baseAmount.min,
                            max = randomExp.baseAmount.max,
                        };
                        adjustBaseAmount += randomGold.amountIncreaseEachLevel * -(defaultLevel - 1);
                        adjustRandomGold = new IncrementalMinMaxInt()
                        {
                            baseAmount = new MinMaxInt()
                            {
                                min = (int)adjustBaseAmount.min,
                                max = (int)adjustBaseAmount.max,
                            },
                            amountIncreaseEachLevel = randomGold.amountIncreaseEachLevel,
                        };
                    }
                    return adjustRandomGold.Value;
                }
            }
        }

        private readonly List<MonsterSkill> tempRandomSkills = new List<MonsterSkill>();

        public virtual int RandomExp(int level)
        {
            return AdjustedRandomExp.GetAmount(level).Random();
        }

        public virtual int RandomGold(int level)
        {
            return AdjustedRandomGold.GetAmount(level).Random();
        }

        public virtual void RandomItems(System.Action<BaseItem, int> onRandomItem, float rate = 1f)
        {
            if (CacheRandomItems.Count == 0 && CacheItemRandomByWeightTables.Count <= 0)
                return;
            int randomDropCount = 0;
            int i;
            // Drop certain drop rate items
            certainDropItems.Shuffle();
            for (i = 0; i < certainDropItems.Count && randomDropCount < maxDropItems; ++i)
            {
                if (BaseGameNetworkManager.CurrentMapInfo.ExcludeItemFromDropping(certainDropItems[i].item))
                    continue;
                if (certainDropItems[i].minAmount <= 0)
                    onRandomItem.Invoke(certainDropItems[i].item, certainDropItems[i].maxAmount);
                else
                    onRandomItem.Invoke(certainDropItems[i].item, Random.Range(certainDropItems[i].minAmount, certainDropItems[i].maxAmount));
                ++randomDropCount;
            }
            // Reached max drop items?
            if (randomDropCount >= maxDropItems)
                return;
            // Drop uncertain drop rate items
            uncertainDropItems.Shuffle();
            for (i = 0; i < uncertainDropItems.Count && randomDropCount < maxDropItems; ++i)
            {
                if (Random.value > uncertainDropItems[i].dropRate * rate)
                    continue;
                if (BaseGameNetworkManager.CurrentMapInfo.ExcludeItemFromDropping(uncertainDropItems[i].item))
                    continue;
                if (uncertainDropItems[i].minAmount <= 0)
                    onRandomItem.Invoke(uncertainDropItems[i].item, uncertainDropItems[i].maxAmount);
                else
                    onRandomItem.Invoke(uncertainDropItems[i].item, Random.Range(uncertainDropItems[i].minAmount, uncertainDropItems[i].maxAmount));
                ++randomDropCount;
            }
            // Reached max drop items?
            if (randomDropCount >= maxDropItems)
                return;
            // Drop items by weighted tables
            CacheItemRandomByWeightTables.Shuffle();
            for (i = 0; i < CacheItemRandomByWeightTables.Count && randomDropCount < maxDropItems; ++i)
            {
                CacheItemRandomByWeightTables[i].RandomItem(onRandomItem);
            }
        }

        public virtual CurrencyAmount[] RandomCurrencies()
        {
            if (CacheRandomCurrencies.Count == 0)
                return new CurrencyAmount[0];
            List<CurrencyAmount> currencies = new List<CurrencyAmount>();
            CurrencyRandomAmount randomCurrency;
            for (int count = 0; count < CacheRandomCurrencies.Count; ++count)
            {
                randomCurrency = CacheRandomCurrencies[count];
                currencies.Add(new CurrencyAmount()
                {
                    currency = randomCurrency.currency,
                    amount = Random.Range(randomCurrency.minAmount, randomCurrency.maxAmount),
                });
            }
            return currencies.ToArray();
        }

        public virtual bool RandomSkill(BaseMonsterCharacterEntity entity, out BaseSkill skill, out int level)
        {
            skill = null;
            level = 1;

            if (!entity.CanUseSkill())
                return false;

            if (skills == null || skills.Length == 0)
                return false;

            if (tempRandomSkills.Count != skills.Length)
            {
                tempRandomSkills.Clear();
                tempRandomSkills.AddRange(skills);
            }

            float random = Random.value;
            foreach (MonsterSkill monsterSkill in tempRandomSkills)
            {
                if (monsterSkill.skill == null)
                    continue;

                if (random < monsterSkill.useRate && (monsterSkill.useWhenHpRate <= 0 || entity.HpRate <= monsterSkill.useWhenHpRate))
                {
                    skill = monsterSkill.skill;
                    level = monsterSkill.level;
                    // Shuffle for next random
                    tempRandomSkills.Shuffle();
                    return true;
                }
            }
            return false;
        }

        public override void PrepareRelatesData()
        {
            base.PrepareRelatesData();
            DamageInfo.PrepareRelatesData();
            GameInstance.AddItems(CacheRandomItems);
            if (itemRandomByWeightTables != null)
            {
                foreach (ItemRandomByWeightTable entry in itemRandomByWeightTables)
                {
                    GameInstance.AddItems(entry.randomItems);
                }
            }
        }

        public override bool Validate()
        {
            bool hasChanges = false;
            if (randomExpMin != 0 ||
                randomExpMax != 0)
            {
                hasChanges = true;
                if (randomExp.baseAmount.min == 0 &&
                    randomExp.baseAmount.max == 0 &&
                    randomExp.amountIncreaseEachLevel.min == 0 &&
                    randomExp.amountIncreaseEachLevel.max == 0)
                {
                    IncrementalMinMaxInt result = randomExp;
                    result.baseAmount.min = randomExpMin;
                    result.baseAmount.max = randomExpMax;
                    randomExp = result;
                }
                randomExpMin = 0;
                randomExpMax = 0;
            }
            if (randomGoldMin != 0 ||
                randomGoldMax != 0)
            {
                hasChanges = true;
                if (randomGold.baseAmount.min == 0 &&
                    randomGold.baseAmount.max == 0 &&
                    randomGold.amountIncreaseEachLevel.min == 0 &&
                    randomGold.amountIncreaseEachLevel.max == 0)
                {
                    IncrementalMinMaxInt result = randomGold;
                    result.baseAmount.min = randomGoldMin;
                    result.baseAmount.max = randomGoldMax;
                    randomGold = result;
                }
                randomGoldMin = 0;
                randomGoldMax = 0;
            }
            if (itemDropTable != null)
            {
                hasChanges = true;
                List<ItemDropTable> tempItemDropTables = new List<ItemDropTable>();
                tempItemDropTables.AddRange(itemDropTables);
                tempItemDropTables.Add(itemDropTable);
                itemDropTables = tempItemDropTables.ToArray();
                itemDropTable = null;
            }
            return hasChanges || base.Validate();
        }
    }
}
