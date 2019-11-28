PART
{
	name = bluedog_cameraLowTech
	module = Part
	author = CobaltWolf; powered by DMagicScienceAnimateGeneric
MODEL
{
	model = MyPartMod/Parts/Science/bluedog_cameraLowTech
}
	scale = 1
	rescaleFactor = 1
	node_attach = 0.0, -0.055, -0.052, 0.0, -1.0, 0.0
	TechRequired = start
	entryCost = 0
	cost = 400
	category = Science
	subcategory = 0
	title = MV-4 "Zufar" Film Camera
	manufacturer = Bluedog Design Bureau
	description = While taking measurements with instruments is all well and good, it is also important to get an idea of what space actually looks like. To that end, we've provided this film camera in a space-friendly casing. The film has to be returned to us in order for it to be developed, so make sure the unit is recovered if you want results. //'
	attachRules = 0,1,0,0,1
	mass = 0.01
	dragModelType = default
	maximum_drag = 0.1
	minimum_drag = 0.1
	angularDrag = 2
	crashTolerance = 12
	maxTemp = 1200 // = 2900
	bulkheadProfiles = srf
	
	tags = camera low science hate
	
	MODULE
	{
		name = DMModuleScienceAnimateGeneric
	//Animation Fields**** Similar to ModuleAnimateGeneric (default values shown)
		animationName = Camera					//Name of your animation - get the name from Unity scene
		animSpeed = 1						//Speed to play animation
		endEventGUIName = Retract				//Title of retract animation event/action group
		showEndEvent = false					//Do you want to show the retract event/action group - only displayed in-flight after deploy event triggered
		startEventGUIName = Deploy				//Title of deploy animation event/action group
		showStartEvent = false					//Do you want to show the deploy event/action group
		toggleEventGUIName = Toggle Aperture			//Title of toggle animation event/action group - Plays deploy or retract animation based on current state - Is reversible while playing
		showToggleEvent = true					//Do you want to show the toggle event/action group - not recommended to be used together with deploy/retract events, too many unnecessary buttons
		showEditorEvents = true					//Do you want to be able to preview the animation in the VAB/SPH
	//Science Experiment Fields**** The same as ModuleScienceExperiment (default values shown)
		collectActionName = Collect Photographs			//Name of the EVA data collection event
		dataIsCollectable = true				//Allow EVA Kerbals to collect science reports from the part
		experimentActionName = Take Photograph			//Name for action group and right-click data collection function
		experimentID = bd_camera				//Experiment name - from the "id = " field in your ScienceDefs.cfg for this experiment
		rerunnable = true					//Can the part be used more than once
		resettable = true					//Does nothing???
		resetActionName = Reset Camera				//Name for action group and right-click reset function
		reviewActionName = Review Photographs			//Name for action group and right-click review data function
		transmitWarningText = This film camera cannot transmit data!
		useActionGroups = True					//Are the VAB/SPH action groups available - does not affect the availability of right-click functions
		useStaging = False					//Control experiment activation through staging (may not actually work)
		xmitDataScalar = 0.0					//Transmission data value, determines the percentage of the baseValue (from your ScienceDefs.cfg) recovered from a transmission
	//Science Experiment - Animation Fields**** (fields are from my magnetometer setup)
		customFailMessage = Cannot take a picture here!
		deployingMessage = Opening aperture and exposing film...
		experimentAnimation = true		//Default = true	//Do you want your experiment to be dependent on the animation playing/already being deployed
		experimentWaitForAnimation = true	//Default = false	//Do you want to wait for the animation to complete before you begin the experiment (or any other arbitrary amount of time)
		keepDeployedMode = 0			//Default = 0		//Determines when to play the retract animation
										//Value of 0 is the stock behavior (mostly) - Parts retract when the experiment is reset or the data is transmitted 
										//Value of 1 - Retracts the part immediately after conducting the experiment - i.e. when the experiment results page appears - *probably better to set up a one way animation
										//Value of 2 - Part will not retract - only manual controls can retract the part
		waitForAnimationTime = -1		//Default = -1 (value set to the length of the animation)
										//Amount of time to wait after deploying the animation before the experiment begins - 0 will begin immediately, -1 waits for the animation to complete (this does take into account animSpeed specified above)
		oneWayAnimation = false			//Default = false	//Do you want the animation to only play in one direction - The animation should begin and end in the same position
		asteroidReports = true		        //Default = false	//Do you want to be able to collect results while landed on and/or near an asteroid
		planetaryMask = 524287			//Default = 524287	//Bitmask defining which planets the experiment can be performed on/around, works everywhere by default
		planetFailMessage = Can't conduct experiment here		//Default = Can't conduct experiment here 		//Message to be displayed if the experiment can't be performed on the current planet/moon'
		experimentsLimit = 4	        	//Default = 1		//Sets the limit for how many experiments can be collected and stored by an individual part
		externalDeploy = true			//Default = false	//Allow the experiment to be triggered by an EVA Kerbal; still requires power if applicable
		excludeAtmosphere = false		//Default = false	//Specify experiments that can only run on planets/moons without an atmosphere
	}
}
