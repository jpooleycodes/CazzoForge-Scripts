﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_Multi
{ 	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    public partial class RTS
    {
        public int calculateProduction(int level, ProductionBuilding buildingType)
        {
            int result = 0;

 
            switch (buildingType)
            {
                case ProductionBuilding.Shack:
                    result = c_baseShackProduction * level;
                    break;
                case ProductionBuilding.House:
                    result = c_baseHouseProduction * level;
                    break;
                case ProductionBuilding.Villa:
                    result = c_baseVillaProduction * level;
                    break;
                case ProductionBuilding.Ironmine:
                    result = c_baseIronProduction * level;
                    break;
                case ProductionBuilding.Scrapyard:
                    result = c_baseScrapProduction * level;
                    break;
            }
            


            return result;
        }

        public int calculateUpgradeCost(int currentCost, ProductionBuilding buildingType)
        {
            int result = 0;


            switch (buildingType)
            {
                case ProductionBuilding.Shack:
                    result = (int)(currentCost * c_shackUpgradeMultiplier);
                    break;
                case ProductionBuilding.House:
                    result = (int)(currentCost * c_houseUpgradeMultiplier);
                    break;
                case ProductionBuilding.Villa:
                    result = (int)(currentCost * c_villaUpgradeMultiplier);
                    break;
                case ProductionBuilding.Scrapyard:
                    result = (int)(currentCost * c_scrapUpgradeMultiplier);
                    break;
                case ProductionBuilding.Ironmine:
                    result = (int)(currentCost * c_ironMineUpgradeMultiplier);
                    break;
            }



            return result;
        }
    }
}
