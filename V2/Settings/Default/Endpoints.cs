using System;
using System.Linq;

namespace ApiParser.V2.Settings.Default
{
    public static class Endpoints
    {
        /// <summary>
        /// The default (authenticated) endpoints that are supported by this library.
        /// </summary>
        public static readonly Endpoint.Endpoint[] Default = GetEndpoints();
        
        /// <summary>
        /// Returns the default (authenticated) endpoints that are supported by this library.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>The default (authenticated) endpoints that are supported by this library.</returns>
        /// <exception cref="SettingsException"></exception>
        public static Endpoint.Endpoint[] GetEndpoints(ParseSettings? settings = null)
        {
            if (!settings.HasValue)
            {
                settings = ParseSettings.Default;
            }

            string combinedIdentifierOptionalInt;
            string combinedIdentifierOptionalIntString;
            string combinedIdentifierIntString;
            string identifierGuid;

            try
            {
                combinedIdentifierOptionalInt = settings.Value.GetCombinedIdentifier(new Type[] { typeof(int) }, true);
                combinedIdentifierOptionalIntString = settings.Value.GetCombinedIdentifier(new Type[] { typeof(int), typeof(string) }, true);
                combinedIdentifierIntString = settings.Value.GetCombinedIdentifier(new Type[] { typeof(int), typeof(string) }, false);
                identifierGuid = settings.Value.GetIdentifier(typeof(Guid));
            }
            catch (SettingsException ex)
            {
                throw new SettingsException($"Unable to construct default endpoints with given settings.", ex);
            }

            string[] names = new string[]
            {
                "Account", // blob (<Account>)
                $"Account.Achievements[{combinedIdentifierOptionalInt}]", // blob (<IApiV2ObjectList<AccountAchievement>>)
                $"Account{settings.Value.EndpointSeparator}Bank[{combinedIdentifierOptionalInt}]", // blob (IApiV2ObjectList<AccountItem>)
                $"Account{settings.Value.EndpointSeparator}BuildStorage[{combinedIdentifierOptionalInt}]", // allExpendable (<AccountBuildStorageSlot>), bulkExpandable (<AccountBuildStorageSlot, int>), paginated (<AccountBuildStorageSlot>)
                $"Account{settings.Value.EndpointSeparator}DailyCrafting[{combinedIdentifierOptionalInt}]", // blob (<IApiV2ObjectList<string>>)
                $"Account{settings.Value.EndpointSeparator}Dungeons[{combinedIdentifierOptionalInt}]", // blob (<IApiV2ObjectList<string>>)
                $"Account{settings.Value.EndpointSeparator}Dyes[{combinedIdentifierOptionalInt}]", // blob (<IApiV2ObjectList<int>>)
                $"Account{settings.Value.EndpointSeparator}Emotes[{combinedIdentifierOptionalInt}]", // blob (<IApiV2ObjectList<string>>)
                $"Account{settings.Value.EndpointSeparator}Finishers[{combinedIdentifierOptionalInt}]", // blob (<IApiV2ObjectList<AccountFinisher>>)
                $"Account{settings.Value.EndpointSeparator}Gliders[{combinedIdentifierOptionalInt}]", // blob (<IApiV2ObjectList<int>>)
                $"Account{settings.Value.EndpointSeparator}Home{settings.Value.EndpointSeparator}Cats[{combinedIdentifierOptionalInt}]", // blob (<IApiV2ObjectList<int>>)
                $"Account{settings.Value.EndpointSeparator}Home{settings.Value.EndpointSeparator}Nodes[{combinedIdentifierOptionalInt}]", // blob (<IApiV2ObjectList<string>>)
                $"Account{settings.Value.EndpointSeparator}Inventory[{combinedIdentifierOptionalInt}]", // blob (<IApiV2ObjectList<AccountItem>>)
                $"Account{settings.Value.EndpointSeparator}LegendaryArmory[{combinedIdentifierOptionalInt}]", // blob (<IApiV2ObjectList<AccountLegendaryArmory>>)
                $"Account{settings.Value.EndpointSeparator}MailCarriers[{combinedIdentifierOptionalInt}]", // blob (<IApiV2ObjectList<int>>)
                $"Account{settings.Value.EndpointSeparator}MapChests[{combinedIdentifierOptionalInt}]", // blob (<IApiV2ObjectList<string>>)
                $"Account{settings.Value.EndpointSeparator}Masteries[{combinedIdentifierOptionalInt}]", // blob (<IApiV2ObjectList<AccountMastery>>)
                $"Account{settings.Value.EndpointSeparator}Mastery{settings.Value.EndpointSeparator}Points", // blob (<AccountMasteryPoints>)
                $"Account{settings.Value.EndpointSeparator}Materials[{combinedIdentifierOptionalInt}]", // blob (IApiV2ObjectList<AccountMaterial>)
                $"Account{settings.Value.EndpointSeparator}Minis[{combinedIdentifierOptionalInt}]", // blob (IApiV2ObjectList<int>)
                $"Account{settings.Value.EndpointSeparator}Mounts{settings.Value.EndpointSeparator}Skins[{combinedIdentifierOptionalInt}]", // blob (IApiV2ObjectList<int>)
                $"Account{settings.Value.EndpointSeparator}Mounts{settings.Value.EndpointSeparator}Types[{combinedIdentifierOptionalInt}]", // blob (IApiV2ObjectList<string>)
                $"Account{settings.Value.EndpointSeparator}Novelties[{combinedIdentifierOptionalInt}]", // blob (IApiV2ObjectList<int>)
                $"Account{settings.Value.EndpointSeparator}Outfits[{combinedIdentifierOptionalInt}]", // blob (IApiV2ObjectList<int>)
                $"Account{settings.Value.EndpointSeparator}Progression[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<AccountProgression>
                $"Account{settings.Value.EndpointSeparator}Pvp{settings.Value.EndpointSeparator}Heroes[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<int>
                $"Account{settings.Value.EndpointSeparator}Raids[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<string>
                $"Account{settings.Value.EndpointSeparator}Recipes[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<int>
                $"Account{settings.Value.EndpointSeparator}Skins[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<int>
                $"Account{settings.Value.EndpointSeparator}Titles[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<int>
                $"Account{settings.Value.EndpointSeparator}Wallet[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<AccountCurrency>
                $"Account{settings.Value.EndpointSeparator}WorldBosses[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<string>

                //"Characters", // allExpendable (<Character>), bulkExpandable (<Character, string>), paginated (<Character>)
                $"Characters[{combinedIdentifierOptionalIntString}]", // optional: Characters(all bulk) - int: derived from Characters (blob Character) - string: blob Character
                // are all part of the Characters[Id] endppoint
                //$"Characters[{INDEX_STRING_IDENTIFIER}]{settings.Value.EndpointSeparator}Backstory", // blob CharactersBackstory
                //$"Characters[{INDEX_STRING_IDENTIFIER}]{settings.Value.EndpointSeparator}BuildTabs", // allExpendable (<CharacterBuildTabSlot>), bulkExpandable (<CharacterBuildTabSlot, int>), paginated (<CharacterBuildTabSlot>)
                //$"Characters[{INDEX_STRING_IDENTIFIER}]{settings.Value.EndpointSeparator}BuildTabs{settings.Value.EndpointSeparator}Active", // blob CharacterBuildTabSlot
                //$"Characters[{INDEX_STRING_IDENTIFIER}]{settings.Value.EndpointSeparator}Core", // blob CharactersCore
                //$"Characters[{INDEX_STRING_IDENTIFIER}]{settings.Value.EndpointSeparator}Crafting", // blob CharactersCrafting
                //$"Characters[{INDEX_STRING_IDENTIFIER}]{settings.Value.EndpointSeparator}Equipment", // blob CharactersEquipment
                //$"Characters[{INDEX_STRING_IDENTIFIER}]{settings.Value.EndpointSeparator}EquipmentTabs", // allExpendable (<CharacterEquipmentTabSlot>), bulkExpandable (<CharacterEquipmentTabSlot, int>), paginated (<CharacterEquipmentTabSlot>)
                //$"Characters[{INDEX_STRING_IDENTIFIER}]{settings.Value.EndpointSeparator}EquipmentTabs{settings.Value.EndpointSeparator}Active", // blob CharacterEquipmentTabSlot
                //$"Characters[{INDEX_STRING_IDENTIFIER}]{settings.Value.EndpointSeparator}HeroPoints", // blob IApiV2ObjectList<string>
                //$"Characters[{INDEX_STRING_IDENTIFIER}]{settings.Value.EndpointSeparator}Inventory", // blob CharactersInventory
                //$"Characters[{INDEX_STRING_IDENTIFIER}]{settings.Value.EndpointSeparator}Quests", // blob IApiV2ObjectList<int>
                //$"Characters[{INDEX_STRING_IDENTIFIER}]{settings.Value.EndpointSeparator}Recipes", // blob CharactersRecipes
                //$"Characters[{INDEX_STRING_IDENTIFIER}]{settings.Value.EndpointSeparator}Sab", // blob CharactersSab
                //$"Characters[{INDEX_STRING_IDENTIFIER}]{settings.Value.EndpointSeparator}Training", // blob CharactersTraining

                $"Commerce{settings.Value.EndpointSeparator}Delivery", // blob CommerceDelivery
                $"Commerce{settings.Value.EndpointSeparator}Transactions{settings.Value.EndpointSeparator}Current{settings.Value.EndpointSeparator}Buys[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<CommerceTransactionCurrent>, paginated CommerceTransactionCurrent
                $"Commerce{settings.Value.EndpointSeparator}Transactions{settings.Value.EndpointSeparator}Current{settings.Value.EndpointSeparator}Sells[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<CommerceTransactionCurrent>, paginated CommerceTransactionCurrent
                $"Commerce{settings.Value.EndpointSeparator}Transactions{settings.Value.EndpointSeparator}History{settings.Value.EndpointSeparator}Buys[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<CommerceTransactionHistory>, paginated CommerceTransactionHistory
                $"Commerce{settings.Value.EndpointSeparator}Transactions{settings.Value.EndpointSeparator}History{settings.Value.EndpointSeparator}Sells[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<CommerceTransactionHistory>, paginated CommerceTransactionHistory

                $"Guild[{identifierGuid}]", // blob Guild
                // TODO: not true for the current implementation!
                // are all part of the Guild[Id] endpoint
                //$"Guild[{INDEX_GUID_IDENTIFIER}]{settings.Value.EndpointSeparator}Log[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<GuildLog>
                //$"Guild[{INDEX_GUID_IDENTIFIER}]{settings.Value.EndpointSeparator}Members[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<GuildMember>
                //$"Guild[{INDEX_GUID_IDENTIFIER}]{settings.Value.EndpointSeparator}Ranks[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<GuildRank>
                //$"Guild[{INDEX_GUID_IDENTIFIER}]{settings.Value.EndpointSeparator}Stash[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<GuildStashStorage>
                //$"Guild[{INDEX_GUID_IDENTIFIER}]{settings.Value.EndpointSeparator}Storage[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<GuildStorageItem>
                //$"Guild[{INDEX_GUID_IDENTIFIER}]{settings.Value.EndpointSeparator}Teams[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<GuildTeam>
                //$"Guild[{INDEX_GUID_IDENTIFIER}]{settings.Value.EndpointSeparator}Treasury[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<GuildTreasuryItem>
                //$"Guild[{INDEX_GUID_IDENTIFIER}]{settings.Value.EndpointSeparator}Upgrades[{combinedIdentifierOptionalInt}]", // blob IApiV2ObjectList<int>

                $"Pvp{settings.Value.EndpointSeparator}Games[{combinedIdentifierOptionalInt}]", // allExpendable (<PvpGame>), bulkExpandable (<PvpGame, Guid>), paginated (<PvpGame>)
                $"Pvp{settings.Value.EndpointSeparator}Standings[{combinedIdentifierOptionalInt}]", // blob ApiV2BaseObjectList<PvpStanding>
                $"Pvp{settings.Value.EndpointSeparator}Stats" // blob PvpStats
            };

            return names.Select(name => new Endpoint.Endpoint(name, settings.Value)).ToArray();
        }
    }
}
