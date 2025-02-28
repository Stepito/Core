﻿using BrokeProtocol.Collections;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BPEssentials.Cooldowns
{
    public class CooldownHandler
    {
        public Dictionary<string, Dictionary<string, Dictionary<string, int>>> Cooldowns { get; set; } = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();

        private bool ready;

        public string CustomDataKey { get; } = "bpe:cooldowns";

        public void Load()
        {
            foreach (var player in Core.Instance.SvManager.Database.Users.FindAll().ToDictionary(x => x.ID, x => x.Character.CustomData))
            {
                if (player.Value.Data == null || !player.Value.Data.ContainsKey(CustomDataKey))
                {
                    continue;
                }

                var cooldownsObj = player.Value.FetchCustomData<Dictionary<string, Dictionary<string, int>>>(CustomDataKey);
                Cooldowns.Add(player.Key, cooldownsObj);
                foreach (var cooldownType in cooldownsObj)
                {
                    foreach (var cooldown in cooldownType.Value)
                    {
                        AddCooldown(player.Key, cooldownType.Key, cooldown.Key);
                    }
                }
            }
            ready = true;
        }


        public void SaveCooldowns()
        {
            foreach (var cooldownPlayer in Cooldowns)
            {
                var onlinePlayer = EntityCollections.Humans.FirstOrDefault(x => x.steamID == cooldownPlayer.Key);
                if (onlinePlayer)
                {
                    onlinePlayer.svPlayer.CustomData.AddOrUpdate(CustomDataKey, cooldownPlayer.Value);
                }
                else
                {
                    var newUser = Core.Instance.SvManager.Database.Users.FindById(cooldownPlayer.Key);
                    newUser.Character.CustomData.AddOrUpdate(CustomDataKey, cooldownPlayer.Value);
                    Core.Instance.SvManager.Database.Users.Update(newUser);
                }
            }
        }

        private IEnumerator StartCooldown(string ID, string type, string key)
        {
            while (!ready)
            {
                yield return new WaitForSecondsRealtime(1);
            }

            while (Cooldowns[ID][type][key] > 0)
            {
                --Cooldowns[ID][type][key];
                yield return new WaitForSecondsRealtime(1);
            }
            if (Cooldowns[ID][type].ContainsKey(key))
            {
                Cooldowns[ID][type].Remove(key);
            }

        }

        public void AddCooldown(string ID, string type, string key, int time = 0)
        {
            if (!Cooldowns.ContainsKey(ID))
            {
                Cooldowns.Add(ID, new Dictionary<string, Dictionary<string, int>>());
            }
            if (!Cooldowns[ID].ContainsKey(type))
            {
                Cooldowns[ID].Add(type, new Dictionary<string, int>());
            }
            if (!Cooldowns[ID][type].ContainsKey(key))
            {
                Cooldowns[ID][type].Add(key, time);
            }
            Core.Instance.SvManager.StartCoroutine(StartCooldown(ID, type, key));
        }

        public bool IsCooldown(string ID, string type, string key)
        {
            return GetCooldown(ID, type, key) > 0;
        }

        public int GetCooldown(string ID, string type, string key)
        {
            if (!Cooldowns.ContainsKey(ID) || !Cooldowns[ID].ContainsKey(type) || !Cooldowns[ID][type].ContainsKey(key)) { return 0; }
            return Cooldowns[ID][type][key];
        }
    }
}
