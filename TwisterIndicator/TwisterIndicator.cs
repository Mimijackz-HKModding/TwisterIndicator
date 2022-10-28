using Modding;
using System;
using UnityEngine;
using HutongGames.PlayMaker;
using Modding.Menu;
using System.Collections.Generic;
using Satchel.BetterMenus;
using Newtonsoft.Json;

namespace TwisterIndicator
{
    public class TwisterGlobalSettings
    {
        public bool twisterVisible = false;
        public int colorType = 0;
        public float r, g, b = 1;
        [JsonIgnore]
        public Color customColor = new Color(1, 1, 1);

        public void saveColor()
        {
            r = customColor.r;
            g = customColor.g;
            b = customColor.b;
        }
        public void loadColor()
        {
            customColor = new Color(r, g, b);
        }
    }
    public class ColorHSV
    {
        public float H = 1;
        public float S = 1;
        public float V = 1;
        public ColorHSV(float h, float s, float v)
        {
            H = h;
            S = s;
            V = v;
        }
    }
    public class TwisterIndicator : Mod, ITogglableMod, ICustomMenuMod, IGlobalSettings<TwisterGlobalSettings>
    {
        private static TwisterIndicator _instance;


        internal static TwisterIndicator Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"An instance of {nameof(TwisterIndicator)} was never constructed");
                }
                return _instance;
            }
        }

        public bool ToggleButtonInsideMenu => true;
        private Menu MenuRef;
        private Element[] customColorElements = new Element[4];

        public TwisterGlobalSettings settings = new TwisterGlobalSettings();
        //public bool twisterVisible = false;
        //public int colorType = 0;
        public Color[] typeColors = new Color[]
        {
            new Color(0.7f, 0.7f, 0.7f),
            new Color(1, 1, 1),
            new Color(0, 0, 0)
        };


        public Color twisterColor = new Color(0.7f, 0.7f, 0.7f);
        Color normalColor;
        public int twisterAmount = 24;
        public FsmColor ColorVar;
        public PlayMakerFSM soulHud;

        private int maxMenuColorValues = 255;
        private int maxMenuHueValues = 360;

        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        public TwisterIndicator() : base("Twister Indicator")
        {
            _instance = this;
        }

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? modtoggledelegates)
        {
            //Create a new MenuRef if it's not null
            MenuRef ??= new Menu(
                "Twister Indicator", //the title of the menu screen, it will appear on the top center of the screen 
                new Element[]
                {
                    modtoggledelegates?.CreateToggle("Enabled", ""),
                    new HorizontalOption(
                        "Visible without Spell Twister",
                        "If the visual should be visible even if you haven't equipped Spell Twister",
                        new string[] {"Off", "On"},
                        opt => {
                            settings.twisterVisible = opt switch {
                                0 => false,
                                1 => true,
                                // This should never be called
                                _ => throw new InvalidOperationException()
                            };
                            UpdateBar(true);
                        },
                        () => settings.twisterVisible switch {
                            false => 0,
                            true => 1,
                        }
                    ),
                    new HorizontalOption(
                        "Soul color", "The color to be displayed when you have enough soul, choose custom to make your own HSV color",
                        new string[] {
                            "Gray",
                            "White",
                            "Custom"
                        },
                        opt =>
                        {
                            settings.colorType = opt;
                            UpdateColor(opt);
                            if (opt == 2)
                            {
                                MenuRef.Find("H").Show();
                                MenuRef.Find("S").Show();
                                MenuRef.Find("V").Show();
                            }else
                            {
                                MenuRef.Find("H").Hide();
                                MenuRef.Find("S").Hide();
                                MenuRef.Find("V").Hide();
                            }
                        },
                        () => settings.colorType
                    ),
                    new CustomSlider(
                        "Custom color hue",
                        opt =>
                        {
                            Color.RGBToHSV(this.settings.customColor, out float H, out float S, out float V);
                            this.settings.customColor = Color.HSVToRGB(opt / maxMenuHueValues, S, V);
                            UpdateColor(2);
                        },
                        () => getHSVValues(this.settings.customColor).H * maxMenuHueValues,
                        0,
                        maxMenuHueValues,
                        true,
                        "H"
                    ),
                    new CustomSlider(
                        "Custom color saturation",
                        opt =>
                        {
                            Color.RGBToHSV(this.settings.customColor, out float H, out float S, out float V);
                            this.settings.customColor = Color.HSVToRGB(H, opt / maxMenuColorValues, V);
                            UpdateColor(2);
                        },
                        () => getHSVValues(this.settings.customColor).S * maxMenuColorValues,
                        0,
                        maxMenuColorValues,
                        true,
                        "S"
                    ),
                    new CustomSlider(
                        "Custom color value",
                        opt =>
                        {
                            Color.RGBToHSV(this.settings.customColor, out float H, out float S, out float V);
                            this.settings.customColor = Color.HSVToRGB(H, S, opt / maxMenuColorValues);
                            UpdateColor(2);
                        },
                        () => getHSVValues(this.settings.customColor).V * maxMenuColorValues,
                        0,
                        maxMenuColorValues,
                        true,
                        "V"
                    )
                }
            );

            if (this.settings.colorType == 2)
            {
                /*MenuRef?.Find("H")?.Show();
                MenuRef?.Find("S")?.Show();
                MenuRef?.Find("V")?.Show();*/
                MenuRef.Find("H").isVisible = true;
                MenuRef.Find("S").isVisible = true;
                MenuRef.Find("V").isVisible = true;
            }
            else
            {
                /*MenuRef?.Find("H")?.Hide();
                MenuRef?.Find("S")?.Hide();
                MenuRef?.Find("V")?.Hide();*/
                MenuRef.Find("H").isVisible = false;
                MenuRef.Find("S").isVisible = false;
                MenuRef.Find("V").isVisible = false;
            }


            //uses the GetMenuScreen function to return a menuscreen that MAPI can use. 
            //The "modlistmenu" that is passed into the parameter can be any menuScreen that you want to return to when "Back" button or "esc" key is pressed 
            return MenuRef.GetMenuScreen(modListMenu);
        }

        /*public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            List <IMenuMod.MenuEntry> menu = new List<IMenuMod.MenuEntry>
            {
                toggleButtonEntry.Value,
                new IMenuMod.MenuEntry {
                    Name = "Soul color",
                    Description = "The color to be displayed when you have enough soul",
                    Values = new string[] {
                        "Gray",
                        "White",
                        "Red",
                        "Green",
                        "Blue"
                    },
                    // opt will be the index of the option that has been chosen
                    Saver = opt => {this.settings.colorType = opt; UpdateColor(opt); },
                    Loader = () => this.settings.colorType
                },
                new IMenuMod.MenuEntry {
                    Name = "Visible without Spell Twister",
                    Description = "If the visual should be visible even if you haven't equipped Spell Twister",
                    Values = new string[] {
                        "Off",
                        "On"
                    },
                    Saver = opt => { this.settings.twisterVisible = opt switch {
                        0 => false,
                        1 => true,
                        // This should never be called
                        _ => throw new InvalidOperationException()
                    }; UpdateBar(true); },
                    Loader = () => this.settings.twisterVisible switch {
                        false => 0,
                        true => 1,
                    }
                }
            };
            if (settings.colorType == 5)
            {
                menu.Add(new IMenuMod.MenuEntry
                {
                    Name = "Custom color red"
                });
            }
            return menu;
        }*/

        // if you need preloads, you will need to implement GetPreloadNames and use the other signature of Initialize.
        public override void Initialize()
        {
            Log("Initializing");

            // put additional initialization logic here
            ModHooks.HeroUpdateHook += HeroUpdate;
            On.PlayMakerFSM.OnEnable += FSMEnable;
            twisterColor = UpdatedColor(settings.colorType);

            Log("Initialized");
        }

        private void FSMEnable(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);

            if (self.FsmName == "Liquid Control")
            {
                //Log("Yooo we got it");

                ColorVar = (self.FsmStates[2].Actions[1] as HutongGames.PlayMaker.Actions.EaseColor).toValue;
                //if (settings != null) ColorVar.Value = settings.customColor;
                normalColor = ColorVar.Value;

                soulHud = self;


                /* NOT NECESSARY
                //add new state for can twister
                FsmEvent finishedEvent = self.FsmEvents[0];
                FsmState newState = addTwisterState(self, finishedEvent);
                //modify state cant heal
                FsmState cantHeal = self.FsmStates[2];
                FsmEvent twisterEvent = new FsmEvent("Can Twister");
                self.Fsm.Events = addToArray<FsmEvent>(self.Fsm.Events, twisterEvent);
                //modify variables
                FsmInt mpVariable = new FsmInt("MP");
                FsmInt[] newVariables = new FsmInt[self.FsmVariables.IntVariables.Length + 1];
                self.FsmVariables.IntVariables.CopyTo(newVariables, 0);
                newVariables[(self.FsmVariables.IntVariables.Length - 1) + 1] = mpVariable;
                self.FsmVariables.IntVariables = newVariables;
                //modify transition
                FsmTransition finishedTrans = new FsmTransition();
                finishedTrans.FsmEvent = twisterEvent;
                finishedTrans.ToState = "Can Twister";
                finishedTrans.ToFsmState = newState;
                cantHeal.Transitions = new FsmTransition[1]
                {
                    finishedTrans
                };
                //modify action
                HutongGames.PlayMaker.Actions.GetPlayerDataInt getMP = new HutongGames.PlayMaker.Actions.GetPlayerDataInt();
                FsmOwnerDefault fsmOwnerDefault = new FsmOwnerDefault();
                fsmOwnerDefault.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
                fsmOwnerDefault.GameObject = GameManager.instance.gameObject;
                getMP.gameObject = fsmOwnerDefault;
                getMP.intName = "MPCharge";
                getMP.storeValue = mpVariable;
                HutongGames.PlayMaker.Actions.IntCompare checkTwisterAction = new HutongGames.PlayMaker.Actions.IntCompare();
                checkTwisterAction.integer1 = mpVariable;
                checkTwisterAction.integer2 = twisterAmount;
                checkTwisterAction.equal = twisterEvent;
                checkTwisterAction.lessThan = null;
                checkTwisterAction.greaterThan = twisterEvent;
                FsmStateAction[] newActions = new FsmStateAction[] {
                    cantHeal.Actions[0],
                    getMP,
                    checkTwisterAction,
                    cantHeal.Actions[1],
                    cantHeal.Actions[2]
                };
                cantHeal.Actions.CopyTo(newActions, 0);
                self.FsmStates[2] = cantHeal;
                */

                //Log("ACTUALLY finished!");
            }
        }

        [Obsolete]
        FsmState addTwisterState(PlayMakerFSM self, FsmEvent finishedEvent)
        {
            FsmState cantHeal = self.FsmStates[2];
            FsmState idle = self.FsmStates[0];
            //FsmEvent finishedEvent = new FsmEvent("FINISHED");

            //add state
            FsmState[] newStates = new FsmState[self.FsmStates.Length + 1];
            self.FsmStates.CopyTo(newStates, 0);
            FsmState newState = new FsmState(self.Fsm);

            //modify state
            newState.Fsm = self.Fsm;
            newState.Name = "Can Twister";
            FsmTransition finishedTrans = new FsmTransition();
            finishedTrans.FsmEvent = finishedEvent;
            finishedTrans.ToState = "Idle";
            finishedTrans.ToFsmState = idle;
            newState.Transitions = new FsmTransition[1]
            {
                finishedTrans
            };

            //modify state actions
            newState.Actions = new FsmStateAction[]
            {
                    cantHeal.Actions[1],
                    cantHeal.Actions[2]
            };
            HutongGames.PlayMaker.Actions.EaseColor easeColor = newState.Actions[0] as HutongGames.PlayMaker.Actions.EaseColor;
            easeColor.toValue.Value = twisterColor;
            easeColor.finishEvent = finishedEvent;
            newState.Actions[0] = easeColor;


            newStates[(self.FsmStates.Length - 1) + 1] = newState;

            self.Fsm.States = newStates;
            return newState;
        }
        T[] addToArray<T>(Array addTo, Array added)
        {
            T[] array = new T[addTo.Length + added.Length];
            addTo.CopyTo(array, 0);
            added.CopyTo(array, addTo.Length);
            return array;
        }
        T[] addToArray<T>(Array addTo, T added)
        {
            T[] array = new T[addTo.Length + 1];
            addTo.CopyTo(array, 0);
            array[addTo.Length] = added;
            return array;
        }

        private void HeroUpdate()
        {
            if (ColorVar == null) return;

            if (HeroController.instance.playerData.MPCharge >= 24 && (settings.twisterVisible || HeroController.instance.playerData.equippedCharm_33))
            {
                ColorVar.Value = twisterColor;
            }
            else
            {
                ColorVar.Value = normalColor;
            }
        }
        public Color UpdatedColor(int colorType)
        {
            if (colorType == 2) return settings.customColor;
            return typeColors[colorType];
        }
        public void UpdateColor(int newColorType)
        {
            twisterColor = typeColors[newColorType];
            if (newColorType == 2) twisterColor = settings.customColor;
            UpdateBar(true);
        }
        public void UpdateBar(bool withHeroUpdate)
        {
            if (HeroController.instance == null) return;
            HeroUpdate();


            if (soulHud == null) return;

            if (HeroController.instance.playerData.MPCharge >= 33)
            {
                soulHud.SendEvent("CAN HEAL");
            }
            else
            {
                soulHud.SendEvent("CANT HEAL");
            }
        }
        public void Unload()
        {
            ModHooks.HeroUpdateHook -= HeroUpdate;
            On.PlayMakerFSM.OnEnable -= FSMEnable;
            if (ColorVar != null)
            {
                ColorVar.Value = normalColor;
                UpdateBar(false);
            }
        }

        public void OnLoadGlobal(TwisterGlobalSettings s)
        {
            settings = s;
            settings.loadColor();
            twisterColor = settings.customColor;
        }

        public TwisterGlobalSettings OnSaveGlobal()
        {
            settings.saveColor();
            return settings;
        }
        public ColorHSV getHSVValues(Color rgbColor)
        {
            float H, S, V;
            Color.RGBToHSV(rgbColor, out H, out S, out V);
            return new ColorHSV(H, S, V);
        }
    }
}