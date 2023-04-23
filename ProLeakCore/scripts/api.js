globalState.ProLeakLoadedMods = new Map();

globalState.ProLeak_GetCfg = function(modGUID, cfgName) {
    if (globalState.ProLeakLoadedMods.has(modGUID) === false) {
        console.log("[ProLeak Config] No mod with guid:" + modGUID);
        return;
    }

    const ProLeak_Mod = globalState.ProLeakLoadedMods.get(modGUID);

    if (ProLeak_Mod.configs.has(cfgName) === false) {
        console.log("[ProLeak Config] Mod with guid:" + modGUID + " do not have config:" + cfgName);
        return;
    }

    return ProLeak_Mod.configs.get(cfgName).value;
}

globalState.ProLeak_SetCfg = function(modGUID, cfgName, value) {
    if (globalState.ProLeakLoadedMods.has(modGUID) === false) {
        console.log("[ProLeak Config] No mod with guid:" + modGUID);
        return;
    }

    const ProLeak_Mod = globalState.ProLeakLoadedMods.get(modGUID);

    if (ProLeak_Mod.configs.has(cfgName) === false) {
        console.log("[ProLeak Config] Mod with guid:" + modGUID + " do not have config:" + cfgName);
        return;
    }

    const ProLeak_Mod_Config = ProLeak_Mod.configs.get(cfgName);

    ProLeak_Mod_Config.value = value;

    ProLeak_Mod.configs.set(cfgName, ProLeak_Mod_Config);

    globalState.ProLeakLoadedMods.set(modGUID, ProLeak_Mod);
}

engine.on('ProLeak_AddMod', function(modName, modAuthor, modDescription, modGUID) {

    if (globalState.ProLeakMods.has(modGUID) === true) {
        console.log("[ProLeak Config] Mod with guid:" + modGUID + " already exist and replacing is forbidden");
        return;
    }

    const ProLeak_Mod = {
        name: modName,
        author: modAuthor,
        description: modDescription,
        guid: modGUID,
        configs: new Map()
    };
    globalState.ProLeakLoadedMods.set(modGUID, ProLeak_Mod);

    console.log("[ProLeak Config] Mod with guid:" + modGUID + " added/updated config:" + cfgName);
});

engine.on('ProLeak_AddCfg', function(modGUID, cfgName, cfgValue, cfgAliases) {

    if (globalState.ProLeakLoadedMods.has(modGUID) === false) {
        console.log("[ProLeak Config] No mod with guid:" + modGUID + " to add the config:" + cfgName + " to");
        return;
    }

    const ProLeak_Mod = globalState.ProLeakLoadedMods.get(modGUID);

    if (ProLeak_Mod.configs.has(cfgName) === true) {
        console.log("[ProLeak Config] Mod with guid:" + modGUID + " already have a config with name:" + cfgName);
        return;
    }

    const ProLeak_Mod_Config = {
        name: cfgName,
        value: cfgValue,
        aliases: cfgAliases,
    };

    ProLeak_Mod.configs.set(cfgName, ProLeak_Mod_Config);

    globalState.ProLeakLoadedMods.set(modGUID, ProLeak_Mod);

    console.log("[ProLeak Config] Mod with guid:" + modGUID + " added config named:" + cfgName);
});
