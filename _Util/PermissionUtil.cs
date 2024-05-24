using Gw2Sharp.WebApi.V2.Clients;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ApiParser.AttributeUtil;

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
        private static readonly Dictionary<string, TokenPermission[]> _permissionsByEndpointPath = new Dictionary<string, TokenPermission[]>()
        {
            { GetEndpointPathAttribute(typeof(AccountClient)), new TokenPermission[] { ACCOUNT } },
            { GetEndpointPathAttribute(typeof(AccountAchievementsClient)), new TokenPermission[] { ACCOUNT, PROGRESSION } },
            { GetEndpointPathAttribute(typeof(AccountBankClient)), new TokenPermission[] { ACCOUNT, INVENTORIES } },
            { GetEndpointPathAttribute(typeof(AccountBuildStorageClient)), new TokenPermission[] { ACCOUNT } },
            { GetEndpointPathAttribute(typeof(AccountDailyCraftingClient)), new TokenPermission[] { ACCOUNT, PROGRESSION } },
            { GetEndpointPathAttribute(typeof(AccountDungeonsClient)), new TokenPermission[] { ACCOUNT, PROGRESSION } },
            { GetEndpointPathAttribute(typeof(AccountDyesClient)), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { GetEndpointPathAttribute(typeof(AccountEmotesClient)), new TokenPermission[] { ACCOUNT } },
            { GetEndpointPathAttribute(typeof(AccountFinishersClient)), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { GetEndpointPathAttribute(typeof(AccountGlidersClient)), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { GetEndpointPathAttribute(typeof(AccountHomeCatsClient)), new TokenPermission[] { ACCOUNT, PROGRESSION, UNLOCKS } },
            { GetEndpointPathAttribute(typeof(AccountHomeNodesClient)), new TokenPermission[] { ACCOUNT, PROGRESSION, UNLOCKS } },
            { GetEndpointPathAttribute(typeof(AccountInventoryClient)), new TokenPermission[] { ACCOUNT, INVENTORIES } },
            // not yet implemented in gw2sharp
            //{ GetEndpointPathAttribute(typeof(AccountJadeBotsClient)), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { GetEndpointPathAttribute(typeof(AccountLegendaryArmoryClient)), new TokenPermission[] { ACCOUNT, INVENTORIES, UNLOCKS } },
            // deprecated
            //{ GetEndpointPathAttribute(typeof(AccountLuckClient)), new TokenPermission[] { ACCOUNT, PROGRESSION, UNLOCKS } },
            { GetEndpointPathAttribute(typeof(AccountMailCarriersClient)), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { GetEndpointPathAttribute(typeof(AccountMapChestsClient)), new TokenPermission[] { ACCOUNT, PROGRESSION } },
            { GetEndpointPathAttribute(typeof(AccountMasteriesClient)), new TokenPermission[] { ACCOUNT, PROGRESSION } },
            { GetEndpointPathAttribute(typeof(AccountMasteryPointsClient)), new TokenPermission[] { ACCOUNT, PROGRESSION } },
            { GetEndpointPathAttribute(typeof(AccountMaterialsClient)), new TokenPermission[] { ACCOUNT, INVENTORIES } },
            { GetEndpointPathAttribute(typeof(AccountMinisClient)), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { GetEndpointPathAttribute(typeof(AccountMountsSkinsClient)), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { GetEndpointPathAttribute(typeof(AccountMountsTypesClient)), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { GetEndpointPathAttribute(typeof(AccountNoveltiesClient)), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { GetEndpointPathAttribute(typeof(AccountOutfitsClient)), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { GetEndpointPathAttribute(typeof(AccountProgressionClient)), new TokenPermission[] { PROGRESSION, UNLOCKS } },
            { GetEndpointPathAttribute(typeof(AccountPvpHeroesClient)), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { GetEndpointPathAttribute(typeof(AccountRaidsClient)), new TokenPermission[] { ACCOUNT, PROGRESSION } },
            { GetEndpointPathAttribute(typeof(AccountRecipesClient)), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            // not yet implemented in gw2sharp
            //{ GetEndpointPathAttribute(typeof(AccountSkiffsClient)), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { GetEndpointPathAttribute(typeof(AccountSkinsClient)), new TokenPermission[] { ACCOUNT, UNLOCKS} },
            { GetEndpointPathAttribute(typeof(AccountTitlesClient)), new TokenPermission[] { ACCOUNT, UNLOCKS } },
            { GetEndpointPathAttribute(typeof(AccountWalletClient)), new TokenPermission[] { ACCOUNT, WALLET } },
            // not yet implemented in gw2sharp
            //{ GetEndpointPathAttribute(typeof(AccountWizardsVaultClient)), new TokenPermission[] { ACCOUNT } },
            //{ GetEndpointPathAttribute(typeof(AccountWizardsVaultDailyClient)), new TokenPermission[] { ACCOUNT } },
            //{ GetEndpointPathAttribute(typeof(AccountWizardsVaultListingsClient)), new TokenPermission[] { ACCOUNT } },
            //{ GetEndpointPathAttribute(typeof(AccountWizardsVaultSpecialClient)), new TokenPermission[] { ACCOUNT } },
            //{ GetEndpointPathAttribute(typeof(AccountWizardsVaultWeeklyClient)), new TokenPermission[] { ACCOUNT } },
            { GetEndpointPathAttribute(typeof(AccountWorldBossesClient)), new TokenPermission[] { ACCOUNT, PROGRESSION } },
            { GetEndpointPathAttribute(typeof(CharactersClient)), new TokenPermission[] { ACCOUNT, CHARACTERS } },
            // not explicetly stated in the wiki, but in gw2sharp this is a separate endpoint
            { GetEndpointPathAttribute(typeof(CharactersIdClient)), new TokenPermission[] { ACCOUNT, CHARACTERS } },
            { GetEndpointPathAttribute(typeof(CharactersIdBackstoryClient)), new TokenPermission[] { ACCOUNT, CHARACTERS } },
            { GetEndpointPathAttribute(typeof(CharactersIdBuildTabsClient)), new TokenPermission[] { ACCOUNT, BUILDS, CHARACTERS } },
            // additional sub endpoint
            { GetEndpointPathAttribute(typeof(CharactersIdBuildTabsActiveClient)), new TokenPermission[] { ACCOUNT, BUILDS, CHARACTERS } },
            { GetEndpointPathAttribute(typeof(CharactersIdCoreClient)), new TokenPermission[] { ACCOUNT, CHARACTERS } },
            { GetEndpointPathAttribute(typeof(CharactersIdCraftingClient)), new TokenPermission[] { ACCOUNT, CHARACTERS } },
            // additional sub endpoint
            // deprecated
            //{ GetEndpointPathAttribute(typeof(CharactersIdDungeonsClient)), new TokenPermission[] { ACCOUNT, CHARACTERS } },
            { GetEndpointPathAttribute(typeof(CharactersIdEquipmentClient)), new TokenPermission[] { ACCOUNT, CHARACTERS } },
            { GetEndpointPathAttribute(typeof(CharactersIdEquipmentTabsClient)), new TokenPermission[] { ACCOUNT, BUILDS, CHARACTERS } },
            // additional sub endpoint
            { GetEndpointPathAttribute(typeof(CharactersIdEquipmentTabsActiveClient)), new TokenPermission[] { ACCOUNT, BUILDS, CHARACTERS } },
            { GetEndpointPathAttribute(typeof(CharactersIdHeroPointsClient)), new TokenPermission[] { ACCOUNT, CHARACTERS, PROGRESSION } },
            { GetEndpointPathAttribute(typeof(CharactersIdInventoryClient)), new TokenPermission[] { ACCOUNT, CHARACTERS, INVENTORIES } },
            { GetEndpointPathAttribute(typeof(CharactersIdQuestsClient)), new TokenPermission[] { ACCOUNT, CHARACTERS, PROGRESSION } },
            { GetEndpointPathAttribute(typeof(CharactersIdRecipesClient)), new TokenPermission[] { ACCOUNT, CHARACTERS, INVENTORIES } },
            { GetEndpointPathAttribute(typeof(CharactersIdSabClient)), new TokenPermission[] { ACCOUNT, CHARACTERS } },
            // deprecated in gw2sharp, but not on the API
            //{ GetEndpointPathAttribute(typeof(CharactersIdSkillsClient)), new TokenPermission[] { ACCOUNT, BUILDS, CHARACTERS } },
            //{ GetEndpointPathAttribute(typeof(CharactersIdSpecializations)), new TokenPermission[] { ACCOUNT, BUILDS, CHARACTERS } },
            { GetEndpointPathAttribute(typeof(CharactersIdTrainingClient)), new TokenPermission[] { ACCOUNT, BUILDS, CHARACTERS } },
            // not a valid endpoint
            //{ GetEndpointPathAttribute(typeof(CommerceClient)), new TokenPermission[] { ACCOUNT } },
            { GetEndpointPathAttribute(typeof(CommerceDeliveryClient)), new TokenPermission[] { ACCOUNT, TRADINGPOST } },
            // not implemented as IEndpoint by gw2sharp
            //{ GetEndpointPathAttribute(typeof(CommerceTransactionsClient)), new TokenPermission[] { ACCOUNT, TRADINGPOST } },
            // additional sub endpoint
            // not implemented as IEndpoint by gw2sharp
            //{ GetEndpointPathAttribute(typeof(CommerceTransactionsCurrentClient)), new TokenPermission[] { ACCOUNT, TRADINGPOST } },
            // additional sub endpoint
            { GetEndpointPathAttribute(typeof(CommerceTransactionsCurrentBuysClient)), new TokenPermission[] { ACCOUNT, TRADINGPOST } },
            // additional sub endpoint
            { GetEndpointPathAttribute(typeof(CommerceTransactionsCurrentSellsClient)), new TokenPermission[] { ACCOUNT, TRADINGPOST } },
            // additional sub endpoint
            // not implemented as IEndpoint by gw2sharp
            //{ GetEndpointPathAttribute(typeof(CommerceTransactionsHistoryClient)), new TokenPermission[] { ACCOUNT, TRADINGPOST } },
            // additional sub endpoint
            { GetEndpointPathAttribute(typeof(CommerceTransactionsHistoryBuysClient)), new TokenPermission[] { ACCOUNT, TRADINGPOST } },
            // additional sub endpoint
            { GetEndpointPathAttribute(typeof(CommerceTransactionsHistorySellsClient)), new TokenPermission[] { ACCOUNT, TRADINGPOST } },
            { GetEndpointPathAttribute(typeof(CreateSubtokenClient)), new TokenPermission[] { ACCOUNT } },
            { GetEndpointPathAttribute(typeof(GuildIdLogClient)), new TokenPermission[] { ACCOUNT, GUILDS } },
            { GetEndpointPathAttribute(typeof(GuildIdMembersClient)), new TokenPermission[] { ACCOUNT, GUILDS } },
            { GetEndpointPathAttribute(typeof(GuildIdRanksClient)), new TokenPermission[] { ACCOUNT, GUILDS } },
            { GetEndpointPathAttribute(typeof(GuildIdStashClient)), new TokenPermission[] { ACCOUNT, GUILDS } },
            { GetEndpointPathAttribute(typeof(GuildIdStorageClient)), new TokenPermission[] { ACCOUNT, GUILDS } },
            { GetEndpointPathAttribute(typeof(GuildIdTeamsClient)), new TokenPermission[] { ACCOUNT, GUILDS } },
            { GetEndpointPathAttribute(typeof(GuildIdTreasuryClient)), new TokenPermission[] { ACCOUNT, GUILDS } },
            { GetEndpointPathAttribute(typeof(GuildIdUpgradesClient)), new TokenPermission[] { ACCOUNT, GUILDS } },
            { GetEndpointPathAttribute(typeof(PvpGamesClient)), new TokenPermission[] { ACCOUNT, PVP } },
            { GetEndpointPathAttribute(typeof(PvpStandingsClient)), new TokenPermission[] { ACCOUNT, PVP } },
            { GetEndpointPathAttribute(typeof(PvpStatsClient)), new TokenPermission[] { ACCOUNT, PVP } },
            { GetEndpointPathAttribute(typeof(TokenInfoClient)), new TokenPermission[] { ACCOUNT } }
        };

        /// <summary>
        /// Contains required permissions for all authenticated endpoint paths that are implemented by gw2sharp v1.7.4
        /// </summary>
        public static readonly ReadOnlyDictionary<string, TokenPermission[]> PermissionsByEndpointPath = new ReadOnlyDictionary<string, TokenPermission[]>(_permissionsByEndpointPath);

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

            if (!_permissionsByEndpointPath.ContainsKey(endpointClient.EndpointPath))
            {
                return null;
            }

            return _permissionsByEndpointPath[endpointClient.EndpointPath];
        }
    }
}
