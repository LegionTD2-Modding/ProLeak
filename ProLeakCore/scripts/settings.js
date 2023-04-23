// Rewrites the options content to add the Mods section
function ProLeak_addModSection() {
    var keyId = optionsMenu[optionsMenu.length - 1].key + 1;
    optionsMenu.push({ key: keyId++, menuId: keyId, name: "ProLeak_ModsCfg", displayName: "Mods", content: React.createElement(ModsOptions, {}), narrow: true });
}

// Adds a binding for the new event sent by the handler created in the patched LoadOptions
// It updates directly the legion id
// Saves the value in the globalState
engine.on('ProLeak_setName', function(value) {
    globalState.ProLeak_Name = value;
    console.log("[ProLeak Config] Name:" + globalState.ProLeak_Name);
});

// Hijacks the loadConfig callback of the forceReloadView event
// Forces the use of the new optionsMenu by adding to the original code the call to our MOD_NMM_addModSection()
// It makes sure the Mods tab does not disappear from the options menu when language is changed for ex
// It makes sure to not reset getMastermindVariantsMenu in a similar way
engine.off('forceReloadView');
engine.on('forceReloadView', function () {
    console.log("[ProLeak] Force reloading view...")
    loadConfig();
    ProLeak_addModSection();
});

// Creates the React component that displays the Mods tab
ModsOptions = React.createClass({
    render: function () {
        return (
            React.createElement('ul', { className: 'options-container' },
                React.createElement('h1', { style: { color: "white" } }, "Mods Options"),

                /* Mod infos */
                React.createElement('p', { },
                    React.createElement('span', {
                        style: { color: "#ffcc00" }
                    }, "Narrow Master Minded"),
                    React.createElement('div', { className: 'simple-tooltip flipped-y' },
                        React.createElement('img', {
                            src: 'hud/img/small-icons/help.png', style: {
                                width: '16px',
                                marginLeft: '8px'
                            }
                        }),
                        React.createElement('span', {
                            className: 'tooltiptext',
                            dangerouslySetInnerHTML: {
                                __html: "By Kidev, version 1.4.2<br>This configurable mod makes a single mastermind legion available"
                            }
                        })
                    )
                ),
                /* */

                /* Options */
                React.createElement('li', {},
                    React.createElement('div', { className: 'description' }, "Forced Legion"),
                    React.createElement('div', { className: 'value dropdown' },
                        React.createElement(DropdownMenu, { field: 'MOD_NMM_ForcedMastermindLegion' })
                    )
                )
                /* */
            )
        );
    }
});

// Always for the use of our optionsMenu before the render
ProLeak_addModSection();
