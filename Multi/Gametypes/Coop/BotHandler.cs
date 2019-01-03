﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_Multi
{
    public partial class Coop
    {
        public List<Bot> _bots;
        public Dictionary<ushort, Player> _targetedPlayers;
        public double hpMultiplier = 0.10;
        public int _lastHPChange;

        public void pollBots(int now)
        {
            if (spawnBots && _arena._bGameRunning && now - _lastHPChange >= 120000)
            {
                _lastHPChange = now;
                hpMultiplier = hpMultiplier + 0.07;

                _arena.sendArenaMessage("$The enemy is starting to increase their armor, Stay sharp!", 5);
            }

            if (_bots.Count >= _botMax)
            {
                Log.write(TLog.Warning, "Excessive bot spawning");
                return;
            }

            if (spawnBots && _arena._bGameRunning)
            {
                int flagcount = _flags.Count(f => f.team == _team);

                //Check if we should be spawning any special waves
                checkForWaves(now, flagcount); 

                //Do we need to spawn a dropship?
                if (now - _lastSupplyDrop > _supplyDropDelay)
                {
                    _lastSupplyDrop = now;
                    spawnDropShip(_team);
                }

                //Does our player team need a medic?
                if (now - _lastFriendlyMedic > _medicSpawnDelay)
                {
                    int botcount = _bots.Where(b => b._type.Id == 128 && b._team == _team).Count();
                    int playercount = _team.ActivePlayerCount;
                    int max = Convert.ToInt32(playercount * 0.30);

                    //Always give atleast one...
                    if (playercount == 1 || playercount == 2)
                        max = 1;

                    if (botcount < max)
                    {
                        int add = (max - botcount);
                        _lastFriendlyMedic = now;
                        spawnMedicWave(_team, add);
                    }
                }

                //Does bot team need a marine?
                if (now - _lastMarineWave > _marineSpawnDelay)
                {
                    int botcount = _bots.Where(b => b._type.Id == 131 && b._team == _botTeam).Count();
                    int playercount = _team.ActivePlayerCount;
                    int max = Convert.ToInt32(playercount * 1);

                    if (botcount < max)
                    {
                        int add = (max - botcount);
                        _lastMarineWave = now;
                        spawnMarineWave(_botTeam, add);
                    }
                }

                //Does bot team need a ripper?
                if (now - _lastRipperWave > _ripperSpawnDelay)
                {
                    int botcount = _bots.Where(b => b._type.Id == 145 && b._team == _botTeam).Count();
                    int playercount = _team.ActivePlayerCount;
                    int max = (int)(playercount * 0.50);

                    if (botcount < max)
                    {
                        int add = (max - botcount);
                        _lastMarineWave = now;
                        spawnRipperWave(_botTeam, add);
                    }
                }
            }
        }

     
        private bool newBot(Team team, BotType type, Vehicle target, Player owner, Helpers.ObjectState state = null)
        {
            if (_bots == null)
                _bots = new List<Bot>();

            //What kind is it?
            switch (type)
            {
                #region Dropship
                case BotType.Dropship:
                    {
                        //Collective vehicle
                        ushort vehid = 134;

                        //Titan vehicle?
                        if (team._name == "Titan Militia")
                            vehid = 134;

                        Dropship medic = _arena.newBot(typeof(Dropship), vehid, team, null, state, null) as Dropship;
                        if (medic == null)
                            return false;

                        medic._team = team;
                        medic.type = BotType.Dropship;
                        medic.init(state, _baseScript);

                        medic.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);
                        };

                        _bots.Add(medic);
                    }
                    break;
                #endregion
                #region Gunship
                case BotType.Gunship:
                    {
                        //Collective vehicle
                        ushort vehid = 134;

                        //Titan vehicle?
                        if (team._name == "Titan Militia")
                            vehid = 147;

                        Gunship gunship = _arena.newBot(typeof(Gunship), vehid, team, owner, state, null) as Gunship;
                        if (gunship == null)
                            return false;

                        gunship._team = team;
                        gunship.type = BotType.Dropship;
                        gunship.init(state, _baseScript, target, owner);

                        gunship.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);
                        };

                        _bots.Add(gunship);
                    }
                    break;
                #endregion
                #region Medic
                case BotType.Medic:
                    {
                        //Collective vehicle
                        ushort vehid = 301;

                        //Titan vehicle?
                        if (team._name == "Titan Militia")
                            vehid = 128;

                        Medic medic = _arena.newBot(typeof(Medic), vehid, team, null, state, null) as Medic;

                        if (medic == null)
                            return false;

                        medic._team = team;
                        medic.type = BotType.Medic;
                        medic.init(this);

                        medic.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);

                        };

                        _bots.Add(medic);
                    }
                    break;
                #endregion
                #region Elite Heavy
                case BotType.EliteHeavy:
                    {
                        //Collective vehicle
                        ushort vehid = 148;

                        //Titan vehicle?
                        if (team._name == "Titan Militia")
                            vehid = 128;

                        EliteHeavy heavy = _arena.newBot(typeof(EliteHeavy), vehid, team, null, state, null) as EliteHeavy;

                        if (heavy == null)
                            return false;

                        heavy._team = team;
                        heavy.type = BotType.EliteHeavy;
                        heavy.init();

                        if (hpMultiplier != 0.0)
                            heavy._state.health = Convert.ToInt16(heavy._type.Hitpoints + (heavy._type.Hitpoints * hpMultiplier));

                        heavy.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);

                        };

                        _bots.Add(heavy);
                    }
                    break;
                #endregion
                #region Elite Marine
                case BotType.EliteMarine:
                    {
                        //Collective vehicle
                        ushort vehid = 146;

                        //Titan vehicle?
                        if (team._name == "Titan Militia")
                            vehid = 128;

                        EliteMarine elitemarine = _arena.newBot(typeof(EliteMarine), vehid, team, null, state, null) as EliteMarine;

                        if (elitemarine == null)
                            return false;

                        elitemarine._team = team;
                        elitemarine.type = BotType.EliteHeavy;
                        elitemarine.init();

                        if (hpMultiplier != 0.0)
                            elitemarine._state.health = Convert.ToInt16(elitemarine._type.Hitpoints + (elitemarine._type.Hitpoints * hpMultiplier));

                        elitemarine.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);

                        };

                        _bots.Add(elitemarine);
                    }
                    break;
                #endregion
                #region Marine
                case BotType.Marine:
                    {
                        //Collective vehicle
                        ushort vehid = 131;

                        //Titan vehicle?
                        if (team._name == "Titan Militia")
                            vehid = 133;

                        Marine marine = _arena.newBot(typeof(Marine), vehid, team, null, state, null) as Marine;

                        if (marine == null)
                            return false;

                        marine._team = team;
                        marine.type = BotType.Marine;
                        marine.init();

                        if (hpMultiplier != 0.0)
                            marine._state.health = Convert.ToInt16(marine._type.Hitpoints + (marine._type.Hitpoints * hpMultiplier));

                        marine.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);

                        };

                        _bots.Add(marine);
                    }
                    break;
                #endregion
                #region Ripper
                case BotType.Ripper:
                    {
                        //Collective vehicle
                        ushort vehid = 145;

                        //Titan vehicle?
                        if (team._name == "Titan Militia")
                            vehid = 133;

                        Ripper ripper = _arena.newBot(typeof(Ripper), vehid, team, null, state, null) as Ripper;

                        if (ripper == null)
                            return false;

                        ripper._team = team;
                        ripper.type = BotType.Ripper;
                        ripper.init();

                        if (hpMultiplier != 0.0)
                            ripper._state.health = Convert.ToInt16(ripper._type.Hitpoints + (ripper._type.Hitpoints * hpMultiplier));

                        ripper.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);

                        };

                        _bots.Add(ripper);
                    }
                    break;
                #endregion
                #region ExoLight
                case BotType.ExoLight:
                    {
                        //Collective vehicle
                        ushort vehid = 149;

                        //Titan vehicle?
                        if (team._name == "Titan Militia")
                            vehid = 133;

                        ExoLight exo = _arena.newBot(typeof(ExoLight), vehid, team, null, state, null) as ExoLight;

                        if (exo == null)
                            return false;

                        exo._team = team;
                        exo.type = BotType.ExoLight;
                        exo.init();

                        if (hpMultiplier != 0.0)
                            exo._state.health = Convert.ToInt16(exo._type.Hitpoints + (exo._type.Hitpoints * hpMultiplier));

                        exo.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);

                        };

                        _bots.Add(exo);
                    }
                    break;
                #endregion
                #region ExoHeavy
                case BotType.ExoHeavy:
                    {
                        //Collective vehicle
                        ushort vehid = 430;

                        //Titan vehicle?
                        if (team._name == "Titan Militia")
                            vehid = 133;

                        ExoHeavy exo = _arena.newBot(typeof(ExoHeavy), vehid, team, null, state, null) as ExoHeavy;

                        if (exo == null)
                            return false;

                        exo._team = team;
                        exo.type = BotType.ExoHeavy;
                        exo.init();

                        if (hpMultiplier != 0.0)
                            exo._state.health = Convert.ToInt16(exo._type.Hitpoints + (exo._type.Hitpoints * hpMultiplier));

                        exo.Destroyed += delegate (Vehicle bot)
                        {
                            _bots.Remove((Bot)bot);

                        };

                        _bots.Add(exo);
                    }
                    break;
                    #endregion
            }
            return true;
        }
    }
}
