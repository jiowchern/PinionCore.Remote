
using System;
namespace Regulus.Project.ExiledPrincesses
{
    
    using Regulus.Remoting;

    public interface IUserStatus
    {
        void Ready();
        event Action<UserStatus> StatusEvent;
        Value<long> QueryTime();
        event Action BattleBegin;
        event Action EndBegin;
    }

    public interface IVerify
    {        
        Value<bool> CreateAccount(string name, string password);
        Value<LoginResult> Login(string name, string password);        
        void Quit();        
    };

    public interface ITown
    {
        string[] Maps { get; }
        void ToMap(string map);
        string Name { get; }
        
    };
    
    public interface IAdventure
    {
        event Action<string> ChangeLevels;
        Value<string> QueryLevels();
    }

    public interface IStorage
    {
        Value<AccountInfomation> FindAccountInfomation(string name);
		
        void Add(AccountInfomation ai);        
    }

    public interface IAdventureIdle
    {
        void GoForwar();
    }

    
    public interface IAdventureGo
    {
         event Action<long /*time_tick*/ , float /*position*/ , float /*speed*/> ForwardEvent;
    }

    public interface IAdventureChoice
    {
        string[] Maps {get;}
        string[] Town { get; }

        void GoMap(string map);
        void GoTown(string tone);
    }
    public interface IActor
    {
        int Pretotype { get; }
        int Dex { get; }
        int Int { get; }
        int Hp { get; }
        Strategy Specializes { get; }
    }

    public interface ITeam
    {
        int Id { get; }
        int[] Strategys { get; }
    }


    public interface ICombatController
    {
        Value<CombatSkill[]> QueryEnableSkills();
        Value<CombatSkill[]> QueryIdleSkills();

        void FlipSkill(int activities_sn);
        void EnableSkill(int idle_sn);
    }
}
