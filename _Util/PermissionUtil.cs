using Gw2Sharp.WebApi.V2.Clients;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ApiParser
{
    /// <summary>
    /// Provides utility functions for GW2 api <see cref="IEndpointClient"/> permissions.
    /// </summary>
    public static class PermissionUtil
    {
        private const TokenPermission ACCOUNT = TokenPermission.Account;
        private const TokenPermission PROGRESSION = TokenPermission.Progression;
        private const TokenPermission UNLOCKS = TokenPermission.Unlocks;
        private const TokenPermission INVENTORIES = TokenPermission.Inventories;
        private const TokenPermission WALLET = TokenPermission.Wallet;
        private const TokenPermission CHARACTERS = TokenPermission.Characters;
        private const TokenPermission BUILDS = TokenPermission.Builds;
        private const TokenPermission TRADINGPOST = TokenPermission.Tradingpost;
        private const TokenPermission GUILDS = TokenPermission.Guilds;
        private const TokenPermission PVP = TokenPermission.Pvp;

        // permission info taken from https://wiki.guildwars2.com/wiki/API:API_key
        private static readonly Dictionary<Type, TokenPermission[]> _permissionsByClientType = new Dictionary<Type, TokenPermission[]>()
        {
            { typeof(AccountClient), new TokenPermission[] { ACCOUNT } },
            { typeof(AccountAchievementsClient), new TokenPermission[] { ACCOUNT, PROGRESSION } },
            { typeof(AccountBankClient), new TokenPermission[] { ACCOUNT, INVENTORIES } },
            { typeof(AccountBuildStorageClient), new TokenPermission[] { ACCOUNT } },
            { typeof(AccountDailyCraftingClient), new TokenPermission[] { ACCOUNT, PROGRESSION } },
            { typeof(AccountDungeonsClient), new TokenPermission[] { ACCOUNT, PROGRESSION } },
            { typeof(AccountDyesClient), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { typeof(AccountEmotesClient), new TokenPermission[] { ACCOUNT } },
            { typeof(AccountFinishersClient), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { typeof(AccountGlidersClient), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { typeof(AccountHomeCatsClient), new TokenPermission[] { ACCOUNT, PROGRESSION, UNLOCKS } },
            { typeof(AccountHomeNodesClient), new TokenPermission[] { ACCOUNT, PROGRESSION, UNLOCKS } },
            { typeof(AccountInventoryClient), new TokenPermission[] { ACCOUNT, INVENTORIES } },
            // not yet implemented in gw2sharp
            //{ typeof(AccountJadeBotsClient), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { typeof(AccountLegendaryArmoryClient), new TokenPermission[] { ACCOUNT, INVENTORIES, UNLOCKS } },
            // deprecated
            //{ typeof(AccountLuckClient), new TokenPermission[] { ACCOUNT, PROGRESSION, UNLOCKS } },
            { typeof(AccountMailCarriersClient), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { typeof(AccountMapChestsClient), new TokenPermission[] { ACCOUNT, PROGRESSION } },
            { typeof(AccountMasteriesClient), new TokenPermission[] { ACCOUNT, PROGRESSION } },
            { typeof(AccountMasteryPointsClient), new TokenPermission[] { ACCOUNT, PROGRESSION } },
            { typeof(AccountMaterialsClient), new TokenPermission[] { ACCOUNT, INVENTORIES } },
            { typeof(AccountMinisClient), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { typeof(AccountMountsSkinsClient), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { typeof(AccountMountsTypesClient), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { typeof(AccountNoveltiesClient), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { typeof(AccountOutfitsClient), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { typeof(AccountProgressionClient), new TokenPermission[] { PROGRESSION, UNLOCKS } },
            { typeof(AccountPvpHeroesClient), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { typeof(AccountRaidsClient), new TokenPermission[] { ACCOUNT, PROGRESSION } },
            { typeof(AccountRecipesClient), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            // not yet implemented in gw2sharp
            //{ typeof(AccountSkiffsClient), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { typeof(AccountSkinsClient), new TokenPermission[] { ACCOUNT, UNLOCKS} },
            { typeof(AccountTitlesClient), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { typeof(AccountWalletClient), new TokenPermission[] { ACCOUNT, WALLET } },
            // not yet implemented in gw2sharp
            //{ typeof(AccountWizardsVaultClient), new TokenPermission[] { ACCOUNT } },
            //{ typeof(AccountWizardsVaultDailyClient), new TokenPermission[] { ACCOUNT } },
            //{ typeof(AccountWizardsVaultListingsClient), new TokenPermission[] { ACCOUNT } },
            //{ typeof(AccountWizardsVaultSpecialClient), new TokenPermission[] { ACCOUNT } },
            //{ typeof(AccountWizardsVaultWeeklyClient), new TokenPermission[] { ACCOUNT } },
            { typeof(AccountWorldBossesClient), new TokenPermission[] { ACCOUNT, PROGRESSION } },
            { typeof(CharactersClient), new TokenPermission[] { ACCOUNT, CHARACTERS } },
            // not explicetly stated in the wiki, but in gw2sharp this is a separate endpoint
            { typeof(CharactersIdClient), new TokenPermission[] { ACCOUNT, CHARACTERS } },
            { typeof(CharactersIdBackstoryClient), new TokenPermission[] { ACCOUNT, CHARACTERS } },
            { typeof(CharactersIdBuildTabsClient), new TokenPermission[] { ACCOUNT, BUILDS, CHARACTERS } },
            // additional sub endpoint
            { typeof(CharactersIdBuildTabsActiveClient), new TokenPermission[] { ACCOUNT, BUILDS, CHARACTERS } },
            { typeof(CharactersIdCoreClient), new TokenPermission[] { ACCOUNT, CHARACTERS } },
            { typeof(CharactersIdCraftingClient), new TokenPermission[] { ACCOUNT, CHARACTERS } },
            // additional sub endpoint
            // deprecated
            //{ typeof(CharactersIdDungeonsClient), new TokenPermission[] { ACCOUNT, CHARACTERS } },
            { typeof(CharactersIdEquipmentClient), new TokenPermission[] { ACCOUNT, CHARACTERS } },
            { typeof(CharactersIdEquipmentTabsClient), new TokenPermission[] { ACCOUNT, BUILDS, CHARACTERS } },
            // additional sub endpoint
            { typeof(CharactersIdEquipmentTabsActiveClient), new TokenPermission[] { ACCOUNT, BUILDS, CHARACTERS } },
            { typeof(CharactersIdHeroPointsClient), new TokenPermission[] { ACCOUNT, CHARACTERS, PROGRESSION } },
            { typeof(CharactersIdInventoryClient), new TokenPermission[] { ACCOUNT, CHARACTERS, INVENTORIES } },
            { typeof(CharactersIdQuestsClient), new TokenPermission[] { ACCOUNT, CHARACTERS, PROGRESSION } },
            { typeof(CharactersIdRecipesClient), new TokenPermission[] { ACCOUNT, CHARACTERS, INVENTORIES } },
            { typeof(CharactersIdSabClient), new TokenPermission[] { ACCOUNT, CHARACTERS } },
            // deprecated in gw2sharp, but not on the API
            //{ typeof(CharactersIdSkillsClient), new TokenPermission[] { ACCOUNT, BUILDS, CHARACTERS } },
            //{ typeof(CharactersIdSpecializations), new TokenPermission[] { ACCOUNT, BUILDS, CHARACTERS } },
            { typeof(CharactersIdTrainingClient), new TokenPermission[] { ACCOUNT, BUILDS, CHARACTERS } },
            // not a valid endpoint
            //{ typeof(CommerceClient), new TokenPermission[] { ACCOUNT } },
            { typeof(CommerceDeliveryClient), new TokenPermission[] { ACCOUNT, TRADINGPOST } },
            // not implemented as IEndpoint by gw2sharp
            //{ typeof(CommerceTransactionsClient), new TokenPermission[] { ACCOUNT, TRADINGPOST } },
            // additional sub endpoint
            // not implemented as IEndpoint by gw2sharp
            //{ typeof(CommerceTransactionsCurrentClient), new TokenPermission[] { ACCOUNT, TRADINGPOST } },
            // additional sub endpoint
            { typeof(CommerceTransactionsCurrentBuysClient), new TokenPermission[] { ACCOUNT, TRADINGPOST } },
            // additional sub endpoint
            { typeof(CommerceTransactionsCurrentSellsClient), new TokenPermission[] { ACCOUNT, TRADINGPOST } },
            // additional sub endpoint
            // not implemented as IEndpoint by gw2sharp
            //{ typeof(CommerceTransactionsHistoryClient), new TokenPermission[] { ACCOUNT, TRADINGPOST } },
            // additional sub endpoint
            { typeof(CommerceTransactionsHistoryBuysClient), new TokenPermission[] { ACCOUNT, TRADINGPOST } },
            // additional sub endpoint
            { typeof(CommerceTransactionsHistorySellsClient), new TokenPermission[] { ACCOUNT, TRADINGPOST } },
            { typeof(CreateSubtokenClient), new TokenPermission[] { ACCOUNT } },
            { typeof(GuildIdLogClient), new TokenPermission[] { ACCOUNT, GUILDS } },
            { typeof(GuildIdMembersClient), new TokenPermission[] { ACCOUNT, GUILDS } },
            { typeof(GuildIdRanksClient), new TokenPermission[] { ACCOUNT, GUILDS } },
            { typeof(GuildIdStashClient), new TokenPermission[] { ACCOUNT, GUILDS } },
            { typeof(GuildIdStorageClient), new TokenPermission[] { ACCOUNT, GUILDS } },
            { typeof(GuildIdTeamsClient), new TokenPermission[] { ACCOUNT, GUILDS } },
            { typeof(GuildIdTreasuryClient), new TokenPermission[] { ACCOUNT, GUILDS } },
            { typeof(GuildIdUpgradesClient), new TokenPermission[] { ACCOUNT, GUILDS } },
            { typeof(PvpGamesClient), new TokenPermission[] { ACCOUNT, PVP } },
            { typeof(PvpStandingsClient), new TokenPermission[] { ACCOUNT, PVP } },
            { typeof(PvpStatsClient), new TokenPermission[] { ACCOUNT, PVP } },
            { typeof(TokenInfoClient), new TokenPermission[] { ACCOUNT } }
        };

        /// <summary>
        /// Contains required permissions for all authenticated endpoint paths that are implemented by gw2sharp v1.7.4
        /// </summary>
        public static readonly ReadOnlyDictionary<Type, TokenPermission[]> PermissionsByClientType = new ReadOnlyDictionary<Type, TokenPermission[]>(_permissionsByClientType);

        /// <summary>
        /// Returns an array of <see cref="TokenPermission"/>s, that are needed to access to given <paramref name="endpointClient"/>. 
        /// The resulting array will be empty, if no permissions are required.
        /// </summary>
        /// <param name="endpointClient"></param>
        /// <returns>An array of <see cref="TokenPermission"/>s, that are needed to access to given <paramref name="endpointClient"/>. 
        /// Will return <see langword="null"/>, if the <paramref name="endpointClient"/> is authenticated, but not implemented 
        /// by this library.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="endpointClient"/> is null.</exception>
        public static TokenPermission[] GetPermissions(IEndpointClient endpointClient)
        {
            if (endpointClient == null)
            {
                throw new ArgumentNullException(nameof(endpointClient));
            }
            
            if (!(endpointClient is IAuthenticatedClient authenticatedClient))
            {
                return Array.Empty<TokenPermission>();
            }

            if (!_permissionsByClientType.ContainsKey(endpointClient.GetType()))
            {
                return null;
            }

            return _permissionsByClientType[endpointClient.GetType()];
        }
    }
}
