﻿//Blizzless Project 2022
//Blizzless Project 2022
using System;
//Blizzless Project 2022
using System.Collections.Generic;
//Blizzless Project 2022
using System.Linq;
//Blizzless Project 2022
using bgs.protocol.presence.v1;
//Blizzless Project 2022
using D3.Account;
//Blizzless Project 2022
using D3.Achievements;
//Blizzless Project 2022
using D3.Client;
//Blizzless Project 2022
using D3.OnlineService;
//Blizzless Project 2022
using D3.PartyMessage;
//Blizzless Project 2022
using D3.Profile;
//Blizzless Project 2022
using DiIiS_NA.Core.Extensions;
//Blizzless Project 2022
using DiIiS_NA.Core.Storage;
//Blizzless Project 2022
using DiIiS_NA.Core.Storage.AccountDataBase.Entities;
//Blizzless Project 2022
using DiIiS_NA.LoginServer.Base;
//Blizzless Project 2022
using DiIiS_NA.LoginServer.Battle;
//Blizzless Project 2022
using DiIiS_NA.LoginServer.ChannelSystem;
//Blizzless Project 2022
using DiIiS_NA.LoginServer.GuildSystem;
//Blizzless Project 2022
using DiIiS_NA.LoginServer.Helpers;
//Blizzless Project 2022
using DiIiS_NA.LoginServer.Objects;
//Blizzless Project 2022
using DiIiS_NA.LoginServer.Toons;
//Blizzless Project 2022
using Google.ProtocolBuffers;

namespace DiIiS_NA.LoginServer.AccountsSystem
{
	public class GameAccount : PersistentRPCObject
	{
		private Account _owner;

		public Account Owner
		{
			get
			{
				if (_owner == null)
					_owner = AccountManager.GetAccountByPersistentID(AccountId);
				return _owner;
			}
			set
			{
				lock (DBGameAccount)
				{
					var dbGAcc = DBGameAccount;
					dbGAcc.DBAccount = value.DBAccount;
					DBSessions.SessionUpdate(dbGAcc);
				}
			}
		}

		public ulong AccountId = 0;

		public DBGameAccount DBGameAccount
		{
			get
			{
				return DBSessions.SessionGet<DBGameAccount>(PersistentID);
			}
			set { }
		}

		public EntityId D3GameAccountId
		{
			get
			{
				return EntityId.CreateBuilder().SetIdHigh(BnetEntityId.High).SetIdLow(PersistentID).Build();
			}
		}

		public ByteStringPresenceField<BannerConfiguration> BannerConfigurationField
		{
			get
			{
				return new ByteStringPresenceField<BannerConfiguration>(FieldKeyHelper.Program.D3, FieldKeyHelper.OriginatingClass.GameAccount, 1, 0, BannerConfiguration);
			}
		}


		public ByteStringPresenceField<EntityId> LastPlayedHeroIdField
		{
			get
			{
				var val = new ByteStringPresenceField<EntityId>(FieldKeyHelper.Program.D3, FieldKeyHelper.OriginatingClass.GameAccount, 2, 0)
				{
					Value = LastPlayedHeroId
				};
				return val;
			}
		}

		public IntPresenceField ActivityField
		{
			get
			{
				return new IntPresenceField(FieldKeyHelper.Program.D3, FieldKeyHelper.OriginatingClass.GameAccount, 3, 0, CurrentActivity);
			}
		}

		public ByteStringPresenceField<D3.Guild.GuildSummary> ClanIdField
		{
			get
			{
				var val = new ByteStringPresenceField<D3.Guild.GuildSummary>(FieldKeyHelper.Program.D3, FieldKeyHelper.OriginatingClass.GameAccount, 7, 0);
				val.Value = Clan.Summary;
				return val;
			}
		}

		public StringPresenceField GameVersionField
		{
			get
			{
				return new StringPresenceField(FieldKeyHelper.Program.D3, FieldKeyHelper.OriginatingClass.GameAccount, 11, 0, "2.7.1.22044");
			}
		}


		public EntityId LastPlayedHeroId
		{
			get
			{
				if (CurrentToon == null)
					return Toons.Count > 0 ? Toons.First().D3EntityID : AccountHasNoToons;
				return CurrentToon.D3EntityID;
			}
		}

		public ByteStringPresenceField<bgs.protocol.channel.v1.ChannelId> PartyIdField
		{
			get
			{
				var val = new ByteStringPresenceField<bgs.protocol.channel.v1.ChannelId>(FieldKeyHelper.Program.D3, FieldKeyHelper.OriginatingClass.Party, 1, 0)
				{
					Value = PartyChannelId
				};
				return val;
			}
		}
		/*
		public ByteStringPresenceField<EntityId> PartyIdField
		{
			get
			{
				var val = new ByteStringPresenceField<EntityId>(FieldKeyHelper.Program.D3, FieldKeyHelper.OriginatingClass.Party, 1, 0)
				{
					Value = PartyId
				};
				return val;
			}
		}
		//*/

		public bgs.protocol.channel.v1.ChannelId PartyChannelId
		{
			get
			{
				if (LoggedInClient != null && LoggedInClient.CurrentChannel != null)
				{
					return bgs.protocol.channel.v1.ChannelId.CreateBuilder()
						.SetType(0)
						.SetId((uint)LoggedInClient.CurrentChannel.D3EntityId.IdLow)
						.SetHost(bgs.protocol.ProcessId.CreateBuilder().SetLabel(1).SetEpoch(0))
						.Build();
				}
				else
					return null;
			}
			set
			{
				if (value != null)
					LoggedInClient.CurrentChannel = ChannelManager.GetChannelByChannelId (value);
			}
		}

		public EntityId PartyId
		{
			get
			{
				if (LoggedInClient != null && LoggedInClient.CurrentChannel != null)
				{
					return LoggedInClient.CurrentChannel.D3EntityId;
				}
				else
					return null;
			}
			set
			{
				if (value != null)
					LoggedInClient.CurrentChannel = ChannelManager.GetChannelByEntityId(value);
			}
		}

		public IntPresenceField JoinPermissionField
			= new IntPresenceField(FieldKeyHelper.Program.D3, FieldKeyHelper.OriginatingClass.Party, 2, 0);

		public FourCCPresenceField ProgramField
			= new FourCCPresenceField(FieldKeyHelper.Program.BNet, FieldKeyHelper.OriginatingClass.GameAccount, 3, 0);

		public StringPresenceField CallToArmsField
		{
			get
			{
				return new StringPresenceField(FieldKeyHelper.Program.D3, FieldKeyHelper.OriginatingClass.Party, 3, 0, Owner.BattleTagName);
			}
		}

		public StringPresenceField BattleTagField
		{
			get
			{
				return new StringPresenceField(FieldKeyHelper.Program.BNet, FieldKeyHelper.OriginatingClass.GameAccount, 5, 0, Owner.BattleTag);
			}
		}

		public StringPresenceField GameAccountNameField
		{
			get
			{
				return new StringPresenceField(FieldKeyHelper.Program.BNet, FieldKeyHelper.OriginatingClass.GameAccount, 6, 0, Owner.BnetEntityId.Low.ToString() + "#1");
			}
		}

		public EntityIdPresenceField OwnerIdField
		{
			get
			{
				var val = new EntityIdPresenceField(FieldKeyHelper.Program.BNet, FieldKeyHelper.OriginatingClass.GameAccount, 7, 0);
				val.Value = Owner.BnetEntityId;
				return val;
			}
		}

		public BoolPresenceField GameAccountStatusField = new BoolPresenceField(FieldKeyHelper.Program.BNet, FieldKeyHelper.OriginatingClass.GameAccount, 1, 0, false);

		public int _currentActivity = 0;

		public int CurrentActivity
		{
			get { return _currentActivity; }
			set
			{
				_currentActivity = value;
				ChangedFields.SetPresenceFieldValue(ActivityField);
			}
		}


		public IntPresenceField LastOnlineField
		{
			get
			{
				return new IntPresenceField(FieldKeyHelper.Program.BNet, FieldKeyHelper.OriginatingClass.GameAccount, 4, 0, (long)LastOnline);
			}
		}

		public ulong LastOnline = 1;

		public FieldKeyHelper.Program Program;


		public BannerConfiguration BannerConfiguration
		{
			get
			{
				if (_bannerConfiguration != null)
					return _bannerConfiguration;
				var res = BannerConfiguration.CreateBuilder();
				if (DBGameAccount.Banner == null || DBGameAccount.Banner.Length < 1)
				{
					res = BannerConfiguration.CreateBuilder()
						.SetBannerShape(189701627)
						.SetSigilMain(1494901005)
						.SetSigilAccent(3399297034)
						.SetPatternColor(1797588777)
						.SetBackgroundColor(1797588777)
						.SetSigilColor(2045456409)
						.SetSigilPlacement(1015980604)
						.SetPattern(4173846786)
						.SetUseSigilVariant(true);
					//.SetEpicBanner((uint)StringHashHelper.HashNormal("Banner_Epic_02_Class_Completion"))
					//.SetEpicBanner((uint)StringHashHelper.HashNormal("Banner_Epic_03_PVP_Class_Completion"))
					//.SetEpicBanner((uint)StringHashHelper.HashNormal("Banner_Epic_01_Hardcore"))

					lock (DBGameAccount)
					{
						var dbGAcc = DBGameAccount;
						dbGAcc.Banner = res.Build().ToByteArray();
						DBSessions.SessionUpdate(dbGAcc);
					}
				}
				else
					res = BannerConfiguration.CreateBuilder(BannerConfiguration.ParseFrom(DBGameAccount.Banner));

				_bannerConfiguration = res.Build();
				return _bannerConfiguration;
			}
			set
			{
				_bannerConfiguration = value;
				lock (DBGameAccount)
				{
					var dbGAcc = DBGameAccount;
					dbGAcc.Banner = value.ToByteArray();
					DBSessions.SessionUpdate(dbGAcc);
				}

				ChangedFields.SetPresenceFieldValue(BannerConfigurationField);
			}
		}

		private BannerConfiguration _bannerConfiguration;

		private ScreenStatus _screenstatus = ScreenStatus.CreateBuilder().SetScreen(1).SetStatus(0).Build();

		public ScreenStatus ScreenStatus
		{
			get { return _screenstatus; }
			set
			{
				_screenstatus = value;
				JoinPermissionField.Value = value.Screen;
				ChangedFields.SetPresenceFieldValue(JoinPermissionField);
			}
		}

		/// <summary>
		/// Selected toon for current account.
		/// </summary>

		public string CurrentAHCurrency
		{
			get
			{
				if (CurrentToon.IsHardcore)
					return "D3_GOLD_HC";
				else
					return "D3_GOLD";
			}
			set { }
		}
		public bool Logined = false;
		public bool Setted = false;
		public Toon CurrentToon
		{
			get
			{
				if (_currentToonId == 0) return null;
				return ToonManager.GetToonByLowID(_currentToonId);
			}
			set
			{
				if (value.GameAccount.PersistentID != PersistentID) return; //just in case...
				_currentToonId = value.PersistentID;
				lock (DBGameAccount)
				{
					var dbGAcc = DBGameAccount;
					dbGAcc.LastPlayedHero = value.DBToon;
					DBSessions.SessionUpdate(dbGAcc);
				}

				ChangedFields.SetPresenceFieldValue(LastPlayedHeroIdField);
				ChangedFields.SetPresenceFieldValue(value.HeroClassField);
				ChangedFields.SetPresenceFieldValue(value.HeroLevelField);
				ChangedFields.SetPresenceFieldValue(value.HeroParagonLevelField);
				ChangedFields.SetPresenceFieldValue(value.HeroVisualEquipmentField);
				ChangedFields.SetPresenceFieldValue(value.HeroFlagsField);
				ChangedFields.SetPresenceFieldValue(value.HeroNameField);
				ChangedFields.SetPresenceFieldValue(value.HighestUnlockedAct);
				ChangedFields.SetPresenceFieldValue(value.HighestUnlockedDifficulty);
			}
		}

		private ulong _currentToonId = 0;

		public ulong Gold {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardcoreGold;
				}
				else {
					return this.DBGameAccount.Gold;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardcoreGold = value;
					}
					else {
						dbGA.Gold = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int BloodShards {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardcoreBloodShards;
				}
				else {
					return this.DBGameAccount.BloodShards;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardcoreBloodShards = value;
					}
					else {
						dbGA.BloodShards = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int TotalBloodShards {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardTotalBloodShards;
				}
				else {
					return this.DBGameAccount.TotalBloodShards;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardTotalBloodShards = value;
					}
					else {
						dbGA.TotalBloodShards = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int StashSize {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardcoreStashSize;
				}
				else {
					return this.DBGameAccount.StashSize;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardcoreStashSize = value;
					}
					else {
						dbGA.StashSize = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int SeasonStashSize {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardSeasonStashSize;
				}
				else {
					return this.DBGameAccount.SeasonStashSize;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardSeasonStashSize = value;
					}
					else {
						dbGA.SeasonStashSize = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public ulong PvPTotalKilled {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardPvPTotalKilled;
				}
				else {
					return this.DBGameAccount.PvPTotalKilled;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardPvPTotalKilled = value;
					}
					else {
						dbGA.PvPTotalKilled = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public ulong PvPTotalWins {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardPvPTotalWins;
				}
				else {
					return this.DBGameAccount.PvPTotalWins;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardPvPTotalWins = value;
					}
					else {
						dbGA.PvPTotalWins = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public ulong PvPTotalGold {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardPvPTotalGold;
				}
				else {
					return this.DBGameAccount.PvPTotalGold;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardPvPTotalGold = value;
					}
					else {
						dbGA.PvPTotalGold = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int CraftItem1 {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardCraftItem1;
				}
				else {
					return this.DBGameAccount.CraftItem1;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardCraftItem1 = value;
					}
					else {
						dbGA.CraftItem1 = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int CraftItem2 {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardCraftItem2;
				}
				else {
					return this.DBGameAccount.CraftItem2;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardCraftItem2 = value;
					}
					else {
						dbGA.CraftItem2 = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int CraftItem3 {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardCraftItem3;
				}
				else {
					return this.DBGameAccount.CraftItem3;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardCraftItem3 = value;
					}
					else {
						dbGA.CraftItem3 = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int CraftItem4 {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardCraftItem4;
				}
				else {
					return this.DBGameAccount.CraftItem4;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardCraftItem4 = value;
					}
					else {
						dbGA.CraftItem4 = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int CraftItem5 {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardCraftItem5;
				}
				else {
					return this.DBGameAccount.CraftItem5;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardCraftItem5 = value;
					}
					else {
						dbGA.CraftItem5 = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int BigPortalKey {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardBigPortalKey;
				}
				else {
					return this.DBGameAccount.BigPortalKey;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardBigPortalKey = value;
					}
					else {
						dbGA.BigPortalKey = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int LeorikKey {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardLeorikKey;
				}
				else {
					return this.DBGameAccount.LeorikKey;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardLeorikKey = value;
					}
					else {
						dbGA.LeorikKey = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int VialofPutridness {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardVialofPutridness;
				}
				else {
					return this.DBGameAccount.VialofPutridness;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardVialofPutridness = value;
					}
					else {
						dbGA.VialofPutridness = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int IdolofTerror {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardIdolofTerror;
				}
				else {
					return this.DBGameAccount.IdolofTerror;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardIdolofTerror = value;
					}
					else {
						dbGA.IdolofTerror = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int HeartofFright {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardHeartofFright;
				}
				else {
					return this.DBGameAccount.HeartofFright;
				}
			}
			set {
				var dbGA = this.DBGameAccount; 
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardHeartofFright = value;
					}
					else {
						dbGA.HeartofFright = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int HoradricA1Res {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardHoradricA1;
				}
				else {
					return this.DBGameAccount.HoradricA1;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardHoradricA1 = value;
					}
					else {
						dbGA.HoradricA1 = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int HoradricA2Res {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardHoradricA2;
				}
				else {
					return this.DBGameAccount.HoradricA2;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardHoradricA2 = value;
					}
					else {
						dbGA.HoradricA2 = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int HoradricA3Res {
			get { 
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardHoradricA3;
				}
				else {
					return this.DBGameAccount.HoradricA3;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardHoradricA3 = value;
					}
					else {
						dbGA.HoradricA3 = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int HoradricA4Res {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardHoradricA4;
				}
				else {
					return this.DBGameAccount.HoradricA4;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardHoradricA4 = value;
					}
					else {
						dbGA.HoradricA4 = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public int HoradricA5Res {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardHoradricA5;
				}
				else {
					return this.DBGameAccount.HoradricA5;
				}
			}
			set {
				var dbGA = this.DBGameAccount;
				lock (dbGA) {
					if (this.CurrentToon.IsHardcore) {
						dbGA.HardHoradricA5 = value;
					}
					else {
						dbGA.HoradricA5 = value;
					}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public Guild Clan
		{
			get
			{
				return GuildManager.GetClans().Where(g => g.HasMember(this)).FirstOrDefault();
			}
		}
		public List<Guild> Communities
		{
			get
			{
				return GuildManager.GetCommunities().Where(g => g.HasMember(this)).ToList();
			}
		}

		public List<D3.Guild.InviteInfo> GuildInvites = new List<D3.Guild.InviteInfo>();

		public GameAccountSettings Settings
		{
			get
			{
				GameAccountSettings res = null;
				if (DBGameAccount.UISettings == null || DBGameAccount.UISettings.Length < 1)
				{
					res = GameAccountSettings.CreateBuilder()
						//.SetChatFontSize(8)
						.SetRmtPreferredCurrency("USD")
						.SetRmtLastUsedCurrency("D3_GOLD")
						.AddAutoJoinChannelsDeprecated("D3_GeneralChat")
						.Build();

					lock (DBGameAccount)
					{
						var dbGAcc = DBGameAccount;
						dbGAcc.UISettings = res.ToByteArray();
						DBSessions.SessionUpdate(dbGAcc);
					}
				}
				else
					res = GameAccountSettings.ParseFrom(DBGameAccount.UISettings);

				return res;
			}
			set
			{
				lock (DBGameAccount)
				{
					var dbGAcc = DBGameAccount;
					dbGAcc.UISettings = value.ToByteArray();
					DBSessions.SessionUpdate(dbGAcc);
				}

				ChangedFields.SetPresenceFieldValue(BannerConfigurationField);
			}
		}

		public Preferences Preferences
		{
			get
			{
				Preferences res = null;
				if (DBGameAccount.UIPrefs == null || DBGameAccount.UIPrefs.Length < 1)
				{
					res = Preferences.CreateBuilder()
						.SetVersion(43)
						//.SetFlags2(0x7FFFFFFF)
						//.SetActionBindingWorldmap(D3.Client.ActionBinding.CreateBuilder().SetKey1(48).SetKey2(-1).SetKeyModifierFlags1(0).SetKeyModifierFlags2(0).Build())
						//.SetActionBindingConsole(D3.Client.ActionBinding.CreateBuilder().SetKey1(48).SetKey2(-1).SetKeyModifierFlags1(0).SetKeyModifierFlags2(0).Build())
						//.SetActionBindingVoiceptt(D3.Client.ActionBinding.CreateBuilder().SetKey1(112).SetKey2(-1).SetKeyModifierFlags1(0).SetKeyModifierFlags2(0).Build())
						.Build();

					lock (DBGameAccount)
					{
						var dbGAcc = DBGameAccount;
						dbGAcc.UIPrefs = res.ToByteArray();
						DBSessions.SessionUpdate(dbGAcc);
					}
				}
				else
					res = Preferences.ParseFrom(DBGameAccount.UIPrefs);

				return res;
			}
			set
			{
				lock (DBGameAccount)
				{
					var dbGAcc = DBGameAccount;
					dbGAcc.UIPrefs = value.ToByteArray();
					DBSessions.SessionUpdate(dbGAcc);
				}

				ChangedFields.SetPresenceFieldValue(BannerConfigurationField);
			}
		}

		/// <summary>
		/// Away status
		/// </summary>
		public AwayStatusFlag AwayStatus { get; private set; }

		private List<AchievementUpdateRecord> _achievements = null;
		private List<CriteriaUpdateRecord> _criteria = null;

		public List<AchievementUpdateRecord> Achievements
		{
			get
			{
				if (_achievements == null)
					SetField();
				return _achievements;
			}
			set
			{
				_achievements = value;
			}
		}

		public List<CriteriaUpdateRecord> AchievementCriteria
		{
			get
			{
				if (_criteria == null)
					SetField();
				return _criteria;
			}
			set
			{
				_criteria = value;
			}
		}

		private ClassInfo GetClassInfo(ToonClass className)
		{
			uint playtime = 0;
			uint highestLevel = 1;
			var _toons = DBSessions.SessionQueryWhere<DBToon>(
					dbi =>
					dbi.DBGameAccount.Id == PersistentID
					&& dbi.Class == className).ToList();
			foreach (var toon in _toons)
			{
				playtime += (uint)toon.TimePlayed;
				if (highestLevel < toon.Level) highestLevel = toon.Level;
			}
			return ClassInfo.CreateBuilder()
				.SetPlaytime(playtime)
				.SetHighestLevel(highestLevel)
				//deprecated //.SetHighestDifficulty(highestDifficulty)
				.Build();
		}

		private uint GetHighestHardcoreLevel()
		{
			uint highestLevel = 0;
			var _toons = DBSessions.SessionQueryWhere<DBToon>(
					dbi =>
					dbi.DBGameAccount.Id == PersistentID
					&& dbi.isHardcore == true).ToList();
			foreach (var toon in _toons)
			{
				if (highestLevel < toon.Level) highestLevel = toon.Level;
			}
			return highestLevel;
		}

		public bool InviteToGuild(Guild guild, GameAccount inviter)
		{
			if (guild.IsClan && Clan != null)
				return false;
			else
			{
				var invite = D3.Guild.InviteInfo.CreateBuilder()
					.SetGuildId(guild.PersistentId)
					.SetGuildName(guild.Name)
					.SetInviterId(inviter.PersistentID)
					.SetCategory(guild.Category)
					.SetInviteType(inviter.PersistentID == PersistentID ? 1U : 0U)
					.SetExpireTime(3600);
				if (guild.IsClan) invite.SetGuildTag(guild.Prefix);
				GuildInvites.Add(invite.Build());


				var update = D3.Notification.GuildInvitesListUpdate.CreateBuilder();
				update.SetIsRemoved(false).SetGuildId(guild.PersistentId).SetInvite(invite);

				var notification = bgs.protocol.notification.v1.Notification.CreateBuilder();
				notification.SetSenderId(bgs.protocol.EntityId.CreateBuilder().SetHigh(0UL).SetLow(0UL));
				notification.SetTargetAccountId(Owner.BnetEntityId);
				notification.SetTargetId(BnetEntityId);
				notification.SetType("D3.NotificationMessage");
				notification.AddAttribute(bgs.protocol.Attribute.CreateBuilder()
					.SetName("D3.NotificationMessage.MessageId").SetValue(bgs.protocol.Variant.CreateBuilder().SetIntValue(0)));
				notification.AddAttribute(bgs.protocol.Attribute.CreateBuilder()
					.SetName("D3.NotificationMessage.Payload").SetValue(bgs.protocol.Variant.CreateBuilder().SetMessageValue(update.Build().ToByteString())));

				LoggedInClient.MakeRPC((lid) =>
					bgs.protocol.notification.v1.NotificationListener.CreateStub(LoggedInClient).OnNotificationReceived(new HandlerController() { ListenerId = lid 
					}, notification.Build(), callback => { }));
				return true;
			}
		}

		public AccountProfile Profile
		{
			get
			{
				var dbGAcc = DBGameAccount;
				var profile = AccountProfile.CreateBuilder()
					.SetParagonLevel((uint)dbGAcc.ParagonLevel)
					.SetDeprecatedBestLadderParagonLevel((uint)dbGAcc.ParagonLevel)
					.SetParagonLevelHardcore((uint)dbGAcc.ParagonLevelHardcore)
					.SetBloodShardsCollected((uint)dbGAcc.TotalBloodShards)
					.SetSeasonId(1)
					.AddSeasons(1)
					//deprecated //.SetHighestDifficulty(Convert.ToUInt32(progress[0], 10))
					.SetNumFallenHeroes(3)
					.SetParagonLevelHardcore(0)  // Hardcore Paragon Level
					.SetBountiesCompleted((uint)dbGAcc.TotalBounties) // Executed orders
					.SetLootRunsCompleted(0) // Closed by the Nephalemic Portals
					.SetPvpWins(0)
					.SetPvpTakedowns(0)
					.SetPvpDamage(0)
					.SetMonstersKilled(dbGAcc.TotalKilled) // Killed monsters
					.SetElitesKilled(dbGAcc.ElitesKilled) // Special Enemies Killed
					.SetGoldCollected(dbGAcc.TotalGold) // Collected gold
					.SetHighestHardcoreLevel(0)     // Maximum level in hermetic mode
					.SetHardcoreMonstersKilled(0) // Killed monsters in ger mode
					.SetHighestHardcoreLevel(GetHighestHardcoreLevel())
					.SetClassBarbarian(GetClassInfo(ToonClass.Barbarian))
					.SetClassCrusader(GetClassInfo(ToonClass.Crusader))
					.SetClassDemonhunter(GetClassInfo(ToonClass.DemonHunter))
					.SetClassMonk(GetClassInfo(ToonClass.Monk))
					.SetClassWitchdoctor(GetClassInfo(ToonClass.WitchDoctor))
					.SetClassWizard(GetClassInfo(ToonClass.Wizard))
					.SetClassNecromancer(GetClassInfo(ToonClass.Necromancer));

					
				if (dbGAcc.BossProgress[0] != 0xff) profile.SetHighestBossDifficulty1(dbGAcc.BossProgress[0]);
				if (dbGAcc.BossProgress[1] != 0xff) profile.SetHighestBossDifficulty2(dbGAcc.BossProgress[1]);
				if (dbGAcc.BossProgress[2] != 0xff) profile.SetHighestBossDifficulty3(dbGAcc.BossProgress[2]);
				if (dbGAcc.BossProgress[3] != 0xff) profile.SetHighestBossDifficulty4(dbGAcc.BossProgress[3]);
				if (dbGAcc.BossProgress[4] != 0xff) profile.SetHighestBossDifficulty5(dbGAcc.BossProgress[4]);
				foreach (var hero in Toons)
				{
					profile.AddHeroes(HeroMiniProfile.CreateBuilder()
						.SetHeroName(hero.Name)
						.SetHeroGbidClass((int)hero.ClassID)
						.SetHeroFlags((uint)hero.Flags)
						.SetHeroId((uint)hero.D3EntityID.IdLow)
						.SetHeroLevel(hero.Level)
						.SetHeroVisualEquipment(hero.HeroVisualEquipmentField.Value)
						);
				}
				profile.SetNumFallenHeroes(1);

				return profile.Build();

				//*/
			}
		}

		public static readonly EntityId AccountHasNoToons =
			EntityId.CreateBuilder().SetIdHigh(0).SetIdLow(0).Build();
		
		//Platinum
		public int Platinum {
			get {
				if (this.CurrentToon.IsHardcore) {
					return this.DBGameAccount.HardPlatinum;
				}
				else {
					return this.DBGameAccount.Platinum;
				}
			}
			set {
				lock (this.DBGameAccount) {
				var dbGA = this.DBGameAccount;
				if (this.CurrentToon.IsHardcore) {
					dbGA.HardPlatinum = value;
				}
				else {
					dbGA.Platinum = value;
				}
					DBSessions.SessionUpdate(dbGA);
				}
			}
		}

		public List<Toon> Toons
		{
			get { return ToonManager.GetToonsForGameAccount(this); }
		}

		public GameAccount(DBGameAccount dbGameAccount, List<Core.Storage.AccountDataBase.Entities.DBAchievements> achs = null)
			: base(dbGameAccount.Id)
		{
			//DBGameAccount = dbGameAccount;
			AccountId = dbGameAccount.DBAccount.Id;
			if (dbGameAccount.LastPlayedHero != null)
				_currentToonId = dbGameAccount.LastPlayedHero.Id;
			LastOnline = dbGameAccount.LastOnline;
			var banner = BannerConfiguration; //just pre-loading it

			const ulong bnetGameAccountHigh = ((ulong)EntityIdHelper.HighIdType.GameAccountId) + (0x0100004433);// + (0x0100004433);

			BnetEntityId = bgs.protocol.EntityId.CreateBuilder().SetHigh(bnetGameAccountHigh).SetLow(PersistentID).Build();
			ProgramField.Value = "D3";
		}

		private void SetField()
		{
			Achievements = new List<AchievementUpdateRecord>();
			AchievementCriteria = new List<CriteriaUpdateRecord>();

			var achs = DBSessions.SessionQueryWhere<Core.Storage.AccountDataBase.Entities.DBAchievements>(dbi => dbi.DBGameAccount.Id == PersistentID).ToList();
			foreach (var ach in achs)
			{
				if (ach.AchievementId == 1)
				{
					;
					uint countOfTravels = 0;
					foreach (var criteria in GameServer.AchievementSystem.AchievementManager.UnserializeBytes(ach.Criteria))
					{
						if (criteria == 3367569)
							countOfTravels++;
					}
					AchievementCriteria.Add(CriteriaUpdateRecord.CreateBuilder()
							.SetCriteriaId32AndFlags8(3367569)
							.SetQuantity32(countOfTravels)
							.Build()
							);
				}
				else
				{
					if (ach.CompleteTime != -1)
						Achievements.Add(AchievementUpdateRecord.CreateBuilder()
							.SetAchievementId(ach.AchievementId)//74987243307105)
							.SetCompletion(ach.CompleteTime)//1476016727)
							.Build()
						);

					if (GameServer.AchievementSystem.AchievementManager.UnserializeBytes(ach.Criteria).Count > 0 && ach.CompleteTime == -1)
						foreach (var criteria in GameServer.AchievementSystem.AchievementManager.UnserializeBytes(ach.Criteria))
							AchievementCriteria.Add(CriteriaUpdateRecord.CreateBuilder()
							.SetCriteriaId32AndFlags8(criteria)
							.SetQuantity32(1)
							.Build()
							);

					if (ach.Quantity > 0 && ach.CompleteTime == -1)
						AchievementCriteria.Add(CriteriaUpdateRecord.CreateBuilder()
						.SetCriteriaId32AndFlags8((uint)GameServer.AchievementSystem.AchievementManager.GetMainCriteria(ach.AchievementId))
						.SetQuantity32((uint)ach.Quantity)
						.Build()
						);
				}
			}
		}

		public bool IsOnline
		{
			get { return LoggedInClient != null; }
			set { }
		}

		private BattleClient _loggedInClient;

		public BattleClient LoggedInClient
		{
			get { return _loggedInClient; }
			set
			{
				_loggedInClient = value;

				GameAccountStatusField.Value = IsOnline;

				ulong current_time = (ulong)DateTime.Now.ToExtendedEpoch();


				//checking last online
				var dbAcc = Owner.DBAccount;

				ChangedFields.SetPresenceFieldValue(GameAccountStatusField);
				ChangedFields.SetPresenceFieldValue(LastOnlineField);
				ChangedFields.SetPresenceFieldValue(BannerConfigurationField);
				
				//TODO: Remove this set once delegate for set is added to presence field
				//this.Owner.AccountOnlineField.Value = this.Owner.IsOnline;
				//var operation = this.Owner.AccountOnlineField.GetFieldOperation();
				try
				{
					NotifyUpdate();
				}
				catch { }
				//this.UpdateSubscribers(this.Subscribers, new List<bgs.protocol.presence.v1.FieldOperation>() { operation });
			}
		}

		/// <summary>
		/// GameAccount's flags.
		/// </summary>
		public GameAccountFlags Flags
		{
			get
			{
				return (GameAccountFlags)DBGameAccount.Flags | GameAccountFlags.HardcoreAdventureModeUnlocked;
			}
			set
			{
				lock (DBGameAccount)
				{
					var dbGAcc = DBGameAccount;
					dbGAcc.Flags = (int)value;
					DBSessions.SessionUpdate(dbGAcc);
				}
			}
		}

		public Digest Digest
		{
			get
			{
				Digest.Builder builder = Digest.CreateBuilder().SetVersion(116)
					// 7447=>99, 7728=> 100, 8801=>102, 8296=>105, 8610=>106, 8815=>106, 8896=>106, 9183=>107
					.SetBannerConfiguration(BannerConfiguration)
					//.SetFlags((uint)this.Flags) //1 - Enable Hardcore
					.SetFlags((uint)114)
					.SetLastPlayedHeroId(LastPlayedHeroId)
					.SetRebirthsUsed(0)
					.SetStashTabsRewardedFromSeasons(1)
					.SetSeasonId(1)
					.SetCompletedSoloRift(false)
					.SetChallengeRiftAccountData(D3.ChallengeRifts.AccountData.CreateBuilder()
						.SetLastChallengeRewardEarned(416175).SetLastChallengeTried(416175)
						)
					.AddAltLevels((uint)DBGameAccount.ParagonLevel)
					//.AddAltLevels((uint)this.DBGameAccount.ParagonLevelHardcore)
					;
				if (Clan != null)
					builder.SetGuildId(Clan.PersistentId);

				return builder.Build();
			}
		}

		public uint AchievementPoints
		{
			get
			{
				return (uint)Achievements.Where(a => a.Completion != -1).Count() * 10U;
			}
		}

		#region Notifications

		public override void NotifyUpdate()
		{
			var operations = ChangedFields.GetChangedFieldList();
			ChangedFields.ClearChanged();
			UpdateSubscribers(Subscribers, operations);
		}

		public override List<FieldOperation> GetSubscriptionNotifications()
		{
			//for now set it here
			GameAccountStatusField.Value = IsOnline;

			var operationList = new List<FieldOperation>();

			//gameaccount
			//D3,GameAccount,1,0 -> D3.DBAccount.BannerConfiguration
			//D3,GameAccount,2,0 -> ToonId
			//D3,GameAccount,3,0 -> Activity
			//D3,Party,1,0 -> PartyId
			//D3,Party,2,0 -> JoinPermission
			//D3,Hero,1,0 -> Hero Class
			//D3,Hero,2,0 -> Hero's current level
			//D3,Hero,3,0 -> D3.Hero.VisualEquipment
			//D3,Hero,4,0 -> Hero's flags
			//D3,Hero,5,0 -> Hero Name
			//D3,Hero,6,0 -> HighestUnlockedAct
			//D3,Hero,7,0 -> HighestUnlockedDifficulty
			//Bnet,GameAccount,1,0 -> GameAccount Online
			//Bnet,GameAccount,3,0 -> FourCC = "D3"
			//Bnet,GameAccount,4,0 -> Unk Int (0 if GameAccount is Offline)
			//Bnet,GameAccount,5,0 -> BattleTag
			//Bnet,GameAccount,6,0 -> DBAccount.Low + "#1"
			//Bnet,GameAccount,7,0 -> DBAccount.EntityId
			operationList.Add(BannerConfigurationField.GetFieldOperation());
			if (LastPlayedHeroId != AccountHasNoToons)
			{
				operationList.Add(LastPlayedHeroIdField.GetFieldOperation());
				if (CurrentToon != null)
					operationList.AddRange(CurrentToon.GetSubscriptionNotifications());
			}

			operationList.Add(GameAccountStatusField.GetFieldOperation());
			operationList.Add(ProgramField.GetFieldOperation());
			operationList.Add(LastOnlineField.GetFieldOperation());
			operationList.Add(BattleTagField.GetFieldOperation());
			operationList.Add(GameAccountNameField.GetFieldOperation());
			operationList.Add(OwnerIdField.GetFieldOperation());
			if (Clan != null)
				operationList.Add(ClanIdField.GetFieldOperation());
			operationList.Add(GameVersionField.GetFieldOperation());
			operationList.Add(PartyIdField.GetFieldOperation());
			operationList.Add(JoinPermissionField.GetFieldOperation());
			operationList.Add(CallToArmsField.GetFieldOperation());
			operationList.Add(ActivityField.GetFieldOperation());
			return operationList;
		}

		#endregion

		public void Update(IList<FieldOperation> operations)
		{
			List<FieldOperation> operationsToUpdate = new List<FieldOperation>();
			foreach (var operation in operations)
			{
				switch (operation.Operation)
				{
					case FieldOperation.Types.OperationType.SET:
						var op_build = DoSet(operation.Field);
						if (op_build.HasValue)
						{
							var new_op = operation.ToBuilder();
							new_op.SetField(op_build);
							operationsToUpdate.Add(new_op.Build());
						}
						break;
					case FieldOperation.Types.OperationType.CLEAR:
						DoClear(operation.Field);
						break;
					default:
						Logger.Warn("No operation type.");
						break;
				}
			}
			if (operationsToUpdate.Count > 0)
				UpdateSubscribers(Subscribers, operationsToUpdate);
		}

		public void TestUpdate()
		{
			var operations = GetSubscriptionNotifications();
			/*
			operations.Add(
				FieldOperation.CreateBuilder()
				.SetOperation(FieldOperation.Types.OperationType.SET)
				.SetField(
					Field.CreateBuilder()
					.SetKey(FieldKey.CreateBuilder().SetGroup(4).SetField(3).SetProgram(17459))
					.SetValue(bgs.protocol.Variant.CreateBuilder().SetStringValue("TExt")))
				.Build()
			);
			//*/
			//operations.Add(new StringPresenceField(FieldKeyHelper.Program.D3, FieldKeyHelper.OriginatingClass.Party, 3, 0, "CallToArms").GetFieldOperation());
			//this.Update(operations);
		}

		private Field.Builder DoSet(Field field)
		{
			FieldOperation.Builder operation = FieldOperation.CreateBuilder();

			Field.Builder returnField = Field.CreateBuilder().SetKey(field.Key);
			if (LoggedInClient == null) return returnField;

			switch ((FieldKeyHelper.Program)field.Key.Program)
			{
				case FieldKeyHelper.Program.D3:
					if (field.Key.Group == 2 && field.Key.Field == 3) //CurrentActivity
					{
						CurrentActivity = (int)field.Value.IntValue;
						returnField.SetValue(field.Value);
						Logger.Debug("{0} set CurrentActivity to {1}", this, field.Value.IntValue);
					}
					else if (field.Key.Group == 2 && field.Key.Field == 4) //Unknown bool
					{
						returnField.SetValue(field.Value);
						Logger.Debug("{0} set CurrentActivity to {1}", this, field.Value.BoolValue);
					}
					else if (field.Key.Group == 2 && field.Key.Field == 6) //Flags
					{
						returnField.SetValue(field.Value);
						Logger.Debug("{0} set Flags to {1}", this, field.Value.UintValue);
					}
					else if (field.Key.Group == 2 && field.Key.Field == 8) //?
					{
						returnField.SetValue(field.Value);
					}
					else if (field.Key.Group == 2 && field.Key.Field == 11) //Version
					{
						returnField.SetValue(field.Value);
						Logger.Debug("{0} set Version to {1}", this, field.Value.StringValue);
					}
					else if (field.Key.Group == 4 && field.Key.Field == 1) //PartyId
					{
						if (field.Value.HasMessageValue) //7727 Sends empty SET instead of a CLEAR -Egris
						{
							Channel channel = ChannelManager.GetChannelByChannelId(bgs.protocol.channel.v1.ChannelId.ParseFrom(field.Value.MessageValue));
							//this.PartyId = EntityId.CreateBuilder().SetIdLow(NewChannelID.Id).SetIdHigh(0x600000000000000).Build();
							
							PartyChannelId = bgs.protocol.channel.v1.ChannelId.ParseFrom(field.Value.MessageValue);
							LoggedInClient.CurrentChannel = channel;
							var c = bgs.protocol.channel.v1.ChannelId.ParseFrom(field.Value.MessageValue);
							//returnField.SetValue(bgs.protocol.Variant.CreateBuilder().SetMessageValue(PartyChannelId.ToByteString()).Build());
							returnField.SetValue(bgs.protocol.Variant.CreateBuilder().SetMessageValue(PartyChannelId.ToByteString()).Build());
							//returnField.SetValue(field.Value);



							Logger.Debug("{0} set channel to {1}", this, channel);
						}
						else
						{
							PartyId = null;
							//if(PartyChannelId != null)
							//	returnField.SetValue(bgs.protocol.Variant.CreateBuilder().SetMessageValue(PartyChannelId.ToByteString()).Build());
							//else
							returnField.SetValue(bgs.protocol.Variant.CreateBuilder().SetMessageValue(ByteString.Empty).Build());
							Logger.Debug("Empty-field: {0}, {1}, {2}", field.Key.Program, field.Key.Group, field.Key.Field);
						}
					}
					else if (field.Key.Group == 4 && field.Key.Field == 2) //JoinPermission
					{
						if (ScreenStatus.Screen != field.Value.IntValue)
						{
							ScreenStatus = ScreenStatus.CreateBuilder().SetScreen((int)field.Value.IntValue).SetStatus(0).Build();
							Logger.Debug("{0} set current screen to {1}.", this, field.Value.IntValue);
						}
						returnField.SetValue(field.Value);
					}
					else if (field.Key.Group == 4 && field.Key.Field == 3) //CallToArmsMessage
					{
						Logger.Debug("CallToArmsMessage: {0}, {1}, {2}", field.Key.Group, field.Key.Field, field.Value);
						returnField.SetValue(field.Value);
					}
					else if (field.Key.Group == 4 && field.Key.Field == 4) //Party IsFull
					{
						returnField.SetValue(field.Value);
					}
					else if (field.Key.Group == 5 && field.Key.Field == 5) //Game IsPrivate
					{
						//returnField.SetValue(Variant.CreateBuilder().SetBoolValue(false).Build());
						returnField.SetValue(field.Value);
						Logger.Debug("{0} set Game IsPrivate {1}.", this, field.Value.ToString());
					}
					else
					{
						Logger.Warn("GameAccount: Unknown set-field: {0}, {1}, {2} := {3}", field.Key.Program,
									field.Key.Group, field.Key.Field, field.Value);
					}
					break;
				case FieldKeyHelper.Program.BNet:
					if (field.Key.Group == 2 && field.Key.Field == 2) // SocialStatus
					{
						AwayStatus = (AwayStatusFlag)field.Value.IntValue;
						returnField.SetValue(bgs.protocol.Variant.CreateBuilder().SetIntValue((long)AwayStatus).Build());
						Logger.Debug("{0} set AwayStatus to {1}.", this, AwayStatus);
					}
					else if (field.Key.Group == 2 && field.Key.Field == 8)// RichPresence
					{
						returnField.SetValue((field.Value));
					}
					else if (field.Key.Group == 2 && field.Key.Field == 10) // AFK
					{
						returnField.SetValue(field.Value);
						Logger.Debug("{0} set AFK to {1}.", this, field.Value.BoolValue);
					}
					else
					{
						Logger.Warn("GameAccount: Unknown set-field: {0}, {1}, {2} := {3}", field.Key.Program,
									field.Key.Group, field.Key.Field, field.Value);
					}
					break;
			}

			//We only update subscribers on fields that actually change values.
			return returnField;
		}

		private void DoClear(Field field)
		{
			switch ((FieldKeyHelper.Program)field.Key.Program)
			{
				case FieldKeyHelper.Program.D3:
					Logger.Warn("GameAccount: Unknown clear-field: {0}, {1}, {2}", field.Key.Program, field.Key.Group,
								field.Key.Field);
					break;
				case FieldKeyHelper.Program.BNet:
					Logger.Warn("GameAccount: Unknown clear-field: {0}, {1}, {2}", field.Key.Program, field.Key.Group,
								field.Key.Field);
					break;
			}
		}

		public Field QueryField(FieldKey queryKey)
		{
			Field.Builder field = Field.CreateBuilder().SetKey(queryKey);

			switch ((FieldKeyHelper.Program)queryKey.Program)
			{
				case FieldKeyHelper.Program.D3:
					if (queryKey.Group == 2 && queryKey.Field == 1) // Banner configuration
					{
						field.SetValue(
							bgs.protocol.Variant.CreateBuilder().SetMessageValue(BannerConfigurationField.Value.ToByteString()).Build
								());
					}
					else if (queryKey.Group == 2 && queryKey.Field == 2) //Hero's EntityId
					{
						field.SetValue(bgs.protocol.Variant.CreateBuilder().SetMessageValue(LastPlayedHeroId.ToByteString()).Build());
					}
					else if (queryKey.Group == 2 && queryKey.Field == 4) //Unknown Bool
					{
						field.SetValue(bgs.protocol.Variant.CreateBuilder().SetBoolValue(true).Build());
					}
					else if (queryKey.Group == 2 && queryKey.Field == 8)
					{
						field.SetValue(bgs.protocol.Variant.CreateBuilder().SetBoolValue(true).Build());
					}
					else if (queryKey.Group == 3 && queryKey.Field == 1) // Hero's class (GbidClass)
					{
						field.SetValue(bgs.protocol.Variant.CreateBuilder().SetIntValue(CurrentToon.ClassID).Build());
					}
					else if (queryKey.Group == 3 && queryKey.Field == 2) // Hero's current level
					{
						field.SetValue(bgs.protocol.Variant.CreateBuilder().SetIntValue(CurrentToon.Level).Build());
					}
					else if (queryKey.Group == 3 && queryKey.Field == 3) // Hero's visible equipment
					{
						field.SetValue(
							bgs.protocol.Variant.CreateBuilder().SetMessageValue(
								CurrentToon.HeroVisualEquipmentField.Value.ToByteString()).Build());
					}
					else if (queryKey.Group == 3 && queryKey.Field == 4) // Hero's flags (gender and such)
					{
						field.SetValue(
							bgs.protocol.Variant.CreateBuilder().SetIntValue(/*1073741821*/(uint)(CurrentToon.Flags | ToonFlags.AllUnknowns)).
								Build());
					}
					else if (queryKey.Group == 3 && queryKey.Field == 5) // Toon name
					{
						field.SetValue(bgs.protocol.Variant.CreateBuilder().SetStringValue(CurrentToon.Name).Build());
					}
					else if (queryKey.Group == 3 && queryKey.Field == 6) //highest act
					{
						field.SetValue(bgs.protocol.Variant.CreateBuilder().SetIntValue(400).Build());
					}
					else if (queryKey.Group == 3 && queryKey.Field == 7) //highest difficulty
					{
						field.SetValue(bgs.protocol.Variant.CreateBuilder().SetIntValue(9).Build());
					}
					else if (queryKey.Group == 4 && queryKey.Field == 1) // Channel ID if the client is online
					{
						//field.SetValue(bgs.protocol.Variant.CreateBuilder().SetMessageValue(PartyChannelId.ToByteString()).Build());
						if (PartyId != null)
							field.SetValue(bgs.protocol.Variant.CreateBuilder().SetMessageValue(PartyId.ToByteString()).Build());
						else field.SetValue(bgs.protocol.Variant.CreateBuilder().Build());
					}
					else if (queryKey.Group == 4 && queryKey.Field == 2)
					// Current screen (all known values are just "in-menu"; also see ScreenStatuses sent in ChannelService.UpdateChannelState)
					{
						field.SetValue(bgs.protocol.Variant.CreateBuilder().SetIntValue(ScreenStatus.Screen).Build());
					}
					else if (queryKey.Group == 4 && queryKey.Field == 4) //Unknown Bool
					{
						field.SetValue(bgs.protocol.Variant.CreateBuilder().SetBoolValue(false).Build());
					}
					else
					{
						Logger.Warn("GameAccount Unknown query-key: {0}, {1}, {2}", queryKey.Program, queryKey.Group,
									queryKey.Field);
					}
					break;
				case FieldKeyHelper.Program.BNet:
					if (queryKey.Group == 2 && queryKey.Field == 1) //GameAccount Logged in
					{
						field.SetValue(bgs.protocol.Variant.CreateBuilder().SetBoolValue(GameAccountStatusField.Value).Build());
					}
					else if (queryKey.Group == 2 && queryKey.Field == 2) // Away status
					{
						field.SetValue(bgs.protocol.Variant.CreateBuilder().SetIntValue((long)AwayStatus).Build());
					}
					else if (queryKey.Group == 2 && queryKey.Field == 3) // Program - always D3
					{
						field.SetValue(bgs.protocol.Variant.CreateBuilder().SetFourccValue("D3").Build());
						//field.SetValue(bgs.protocol.Variant.CreateBuilder().SetFourccValue("BNet").Build());
						//BNet = 16974,
						//D3 = 17459,
						//S2 = 21298,
						//WoW = 5730135,
					}
					else if (queryKey.Group == 2 && queryKey.Field == 5) // BattleTag
					{
						field.SetValue(bgs.protocol.Variant.CreateBuilder().SetStringValue(Owner.BattleTag).Build());
					}
					else if (queryKey.Group == 2 && queryKey.Field == 7) // DBAccount.EntityId
					{
						field.SetValue(bgs.protocol.Variant.CreateBuilder().SetEntityIdValue(Owner.BnetEntityId).Build());
					}
					else if (queryKey.Group == 2 && queryKey.Field == 10) // AFK
					{
						field.SetValue(
							bgs.protocol.Variant.CreateBuilder().SetBoolValue(AwayStatus != AwayStatusFlag.Available).Build());
					}
					else
					{
						Logger.Warn("GameAccount Unknown query-key: {0}, {1}, {2}", queryKey.Program, queryKey.Group,
									queryKey.Field);
					}
					break;
			}

			return field.HasValue ? field.Build() : null;
		}

		public override string ToString()
		{
			return $"{{ GameAccount: {Owner.BattleTag} [lowId: {BnetEntityId.Low}] }}";
		}

		//TODO: figure out what 1 and 3 represent, or if it is a flag since all observed values are powers of 2 so far /dustinconrad
		public enum AwayStatusFlag : uint
		{
			Available = 0x00,
			UnknownStatus1 = 0x01,
			Away = 0x02,
			UnknownStatus2 = 0x03,
			Busy = 0x04
		}

		[Flags]
		public enum GameAccountFlags : uint
		{
			None = 0x00,
			HardcoreUnlocked = 0x01,
			AdventureModeUnlocked = 0x04,
			Paragon100 = 0x08,
			MasterUnlocked = 0x10,
			TormentUnlocked = 0x20,
			AdventureModeTutorial = 0x40,
			HardcoreMasterUnlocked = 0x80,
			HardcoreTormentUnlocked = 0x100,
			HardcoreAdventureModeUnlocked = 0x200
		}
	}
}
