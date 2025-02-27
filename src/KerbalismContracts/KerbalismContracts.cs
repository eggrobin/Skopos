﻿using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using KSP.Localization;
using KSP.UI.Screens;
using System.Text;
using KERBALISM;
using System.Reflection;

namespace KerbalismContracts
{
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	public class KerbalismContractsMain : MonoBehaviour
	{
		public static bool fieldVisibilityInitialized = false;

		/// <summary>
		/// We need some time after loading until everything is initialized properly.
		/// F.i. it might take a few seconds until comm net is initalized (which is
		/// a requirement for most of our equipment). Also, KSP itself might need some
		/// time until access to FlightGlobals is possible. This is used for deferred
		/// initialization (5 seconds delay) after load, when this is true everything
		/// should be fine and dandy
		/// </summary>
		public static bool KerbalismInitialized = false;

		public static bool firstStart = true;

		public void Start()
		{
			if (firstStart)
			{
				firstStart = false;

				// this needs to be called to initialize all derivates of KsmPartModule in this plugin
				ModuleData.Init(Assembly.GetExecutingAssembly());

				Configuration.Load();

				API.OnRadiationFieldChanged.Add(RadiationFieldTracker.Update);
				API.OnExperimentStateChanged.Add(ExperimentStateTracker.Update);
				GameEvents.onVesselChange.Add((vessel) => { ExperimentStateTracker.Remove(vessel.id); });
			}
		}
	}

	[KSPScenario(ScenarioCreationOptions.AddToAllGames, new[] { GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT })]
	public sealed class KerbalismContracts : ScenarioModule
	{
		public static KerbalismContracts Instance { get; private set; } = null;
		private readonly Dictionary<int, GlobalRadiationFieldStatus> bodyData = new Dictionary<int, GlobalRadiationFieldStatus>();

		public static readonly EquipmentStateTracker EquipmentStates = new EquipmentStateTracker();

		public static readonly Imaging Imaging = new Imaging();

		private float sunObservationStatusTime;
		private List<Surface> solarSurfaces;

		//  constructor
		public KerbalismContracts()
		{
			// enable global access
			Instance = this;
		}

		private void OnDestroy()
		{
			Instance = null;
		}

		private void Update()
		{
			if (!KerbalismContractsMain.fieldVisibilityInitialized)
			{
				StartCoroutine(InitFieldVisibilityDeferred());
				KerbalismContractsMain.fieldVisibilityInitialized = true;
			}

			if(Time.time - sunObservationStatusTime > 10)
			{
				sunObservationStatusTime = Time.time;
				UpdateSunObservationStatus();
			}
		}

		public void OnGUI()
		{
			Imaging.DrawDebugUI();
		}

		public void FixedUpdate()
		{
			Imaging.Update();
		}

		static readonly Dictionary<CelestialBody, List<Vessel>> vesselsPerSun = new Dictionary<CelestialBody, List<Vessel>>();

		private void UpdateSunObservationStatus()
		{
			// determine sun surface observation status for all suns in the system
			vesselsPerSun.Clear();
			foreach(var entries in EquipmentStates.states)
			{
				foreach(var e in entries.Value)
				{
					if(e.id == Configuration.SunObservationEquipment && e.value == EquipmentState.nominal)
					{
						Vessel v = FlightGlobals.FindVessel(entries.Key);
						if (v != null)
						{
							var sun = Sim.GetParentStar(v.mainBody);
							if (!vesselsPerSun.ContainsKey(sun))
								vesselsPerSun[sun] = new List<Vessel>();
							vesselsPerSun[sun].Add(v);
						}
					}
				}
			}

			foreach (var e in vesselsPerSun)
			{
				var sun = e.Key;
				var vessels = e.Value;
				if (solarSurfaces == null)
					solarSurfaces = BodySurfaceObservation.CreateVisibleSurfaces();
				
				var context = new EvaluationContext(GetUniverseEvaluator(), null, sun);
				context.SetTime(Planetarium.GetUniversalTime());
				Vector3d sunPosition = context.BodyPosition(sun);

				var observedSurface = (float)BodySurfaceObservation.VisibleSurface(vessels, context, sunPosition, Configuration.MinSunObservationAngle, solarSurfaces);
				API.SetStormObservationQuality(sun, observedSurface);
				Utils.LogDebug($"Solar surface observation for {sun.displayName}: {(observedSurface * 100.0).ToString("F2")}%");
			}
		}

		private IUniverseEvaluator evaluator = null;
		public IUniverseEvaluator GetUniverseEvaluator()
		{
			if (evaluator != null)
				return evaluator;

			evaluator = Principia.GetUniverseEvaluator() ?? new StockUniverseEvaluator();

			return evaluator;
		}

		private IEnumerator InitFieldVisibilityDeferred()
		{
			yield return new WaitForSeconds(5);
			InitKerbalism();
		}

		public GlobalRadiationFieldStatus BodyData(CelestialBody body) {
			if(!bodyData.ContainsKey(body.flightGlobalsIndex)) {
				bodyData.Add(body.flightGlobalsIndex, new GlobalRadiationFieldStatus(body.flightGlobalsIndex));
			}
			return bodyData[body.flightGlobalsIndex];
		}

		private static void ShowMessage(CelestialBody body, bool wasVisible, bool visible, RadiationFieldType field)
		{
			if (visible && !wasVisible)
			{
				StringBuilder sb = new StringBuilder(256);
				string message = Localizer.Format("#KerCon_FieldXofYresearched", // <<1>>: <<2>> researched
					Lib.Bold(body.bodyName), Lib.Color(RadiationField.Name(field), Lib.Kolor.Science));
				sb.Append(message);
				sb.Append("\n\n");
				sb.Append(Localizer.Format("#KerCon_FieldResearchedMessage"));

				var bd = Instance.BodyData(body);

				API.Message(sb.ToString());

				MessageSystem.Message m = new MessageSystem.Message("#KerCon_FieldResearched", sb.ToString(), MessageSystemButton.MessageButtonColor.GREEN, MessageSystemButton.ButtonIcons.ACHIEVE);
				MessageSystem.Instance.AddMessage(m);
			}
		}

		public static void SetInnerBeltVisible(CelestialBody body, bool visible = true)
		{
			Utils.LogDebug($"Setting visibility for InnerBelt of {body} to {visible}");
			bool wasVisible = Instance.BodyData(body).inner_visible;
			Instance.BodyData(body).inner_visible = visible;

			API.SetInnerBeltVisible(body, visible);

			ShowMessage(body, wasVisible, visible, RadiationFieldType.INNER_BELT);
		}

		public static void SetOuterBeltVisible(CelestialBody body, bool visible = true)
		{
			Utils.LogDebug($"Setting visibility for OuterBelt of {body} to {visible}");
			bool wasVisible = Instance.BodyData(body).outer_visible;
			Instance.BodyData(body).outer_visible = visible;

			API.SetOuterBeltVisible(body, visible);

			ShowMessage(body, wasVisible, visible, RadiationFieldType.OUTER_BELT);
		}

		public static void SetMagnetopauseVisible(CelestialBody body, bool visible = true)
		{
			Utils.LogDebug($"Setting visibility for magnetosphere of {body} to {visible}");
			bool wasVisible = Instance.BodyData(body).pause_visible;
			Instance.BodyData(body).pause_visible = visible;

			API.SetMagnetopauseVisible(body, visible);

			ShowMessage(body, wasVisible, visible, RadiationFieldType.MAGNETOPAUSE);
		}

		public void InitKerbalism()
		{
			bool isSandboxGame = HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX;

			foreach (var body in FlightGlobals.Bodies)
			{
				var bd = BodyData(body);

				API.SetInnerBeltVisible(body, isSandboxGame || bd.inner_visible || !Configuration.HideRadiationBelts);
				API.SetOuterBeltVisible(body, isSandboxGame || bd.outer_visible || !Configuration.HideRadiationBelts);
				API.SetMagnetopauseVisible(body, isSandboxGame || bd.pause_visible || !Configuration.HideRadiationBelts);

				bd.has_inner = API.HasInnerBelt(body);
				bd.has_outer = API.HasOuterBelt(body);
				bd.has_pause = API.HasMagnetopause(body);
			}
			
			KerbalismContractsMain.KerbalismInitialized = true;
		}

		public override void OnLoad(ConfigNode node)
		{
			KerbalismContractsMain.fieldVisibilityInitialized = false;
			KerbalismContractsMain.KerbalismInitialized = false;

			bodyData.Clear();
			EvaluationContext.Clear();
			
			if (node.HasNode("BodyData"))
			{
				foreach (var body_node in node.GetNode("BodyData").GetNodes())
				{
					var bd = new GlobalRadiationFieldStatus(body_node);
					if (bd != null && bd.index >= 0)
						bodyData.Add(bd.index, bd);
				}
			}

			RadiationFieldTracker.Load(node);
			ExperimentStateTracker.Load(node);
			EquipmentStates.Load(node);
			Imaging.ClearImagers();
		}

		public override void OnSave(ConfigNode node)
		{
			var bodies_node = node.AddNode("BodyData");
			foreach (var p in bodyData)
			{
				p.Value.Save(bodies_node.AddNode("GlobalBodyData"));
			}

			RadiationFieldTracker.Save(node);
			ExperimentStateTracker.Save(node);
			EquipmentStates.Save(node);
		}
	}

	public class GlobalRadiationFieldStatus {
		internal bool inner_visible = false;
		internal bool outer_visible = false;
		internal bool pause_visible = false;
		internal int inner_crossings = 0;
		internal int outer_crossings = 0;
		internal int magneto_crossings = 0;
		internal bool has_inner = false;
		internal bool has_outer = false;
		internal bool has_pause = false;
		internal int index = -1;

		public GlobalRadiationFieldStatus(int index) {
			this.index = index;
		}

		public GlobalRadiationFieldStatus(ConfigNode node) {
			inner_visible = Lib.ConfigValue(node, "inner_visible", false);	
			outer_visible = Lib.ConfigValue(node, "outer_visible", false);
			pause_visible = Lib.ConfigValue(node, "pause_visible", false);
			inner_crossings = Lib.ConfigValue(node, "inner_crossings", 0);
			outer_crossings = Lib.ConfigValue(node, "outer_crossings", 0);
			magneto_crossings = Lib.ConfigValue(node, "magneto_crossings", 0);
			index = Lib.ConfigValue(node, "index", -1);
		}

		public void Save(ConfigNode node) {
			node.AddValue("inner_visible", inner_visible);
			node.AddValue("outer_visible", outer_visible);
			node.AddValue("pause_visible", pause_visible);
			node.AddValue("inner_crossings", inner_crossings);
			node.AddValue("outer_crossings", outer_crossings);
			node.AddValue("magneto_crossings", magneto_crossings);
			node.AddValue("index", index);
		}
	}
}
