namespace VMS.TPS

open System
open System.Windows.Forms
open FsToolkit.ErrorHandling
open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types

module Functions =

    // Utility to show result messages
    let showMessage (text: string) =
        MessageBox.Show(text, "Result", MessageBoxButtons.OK, MessageBoxIcon.Information)
        |> ignore

    // Get the current patient or return a string error
    let getCurrentPatient (context: ScriptContext) =
        match context.Patient with
        | null -> Error "No patient is currently loaded."
        | patient -> Ok patient

    // Get the current course or return a string error
    let getCurrentCourse (context: ScriptContext) =
        match context.Course with
        | null -> Error "No course is currently loaded."
        | course -> Ok course

    // Find the first ExternalPlanSetup with "aCT" in ID, or return an error
    let tryGetMatchingExternalPlan (course: Course) =
        let matchPlan =
            course.ExternalPlanSetups |> Seq.tryFind (fun plan -> plan.Id.Contains("aCT"))

        match matchPlan with
        | Some plan -> Ok plan
        | None -> Error "No external plan with 'aCT' in ID found."

    // Modify an external plan by setting prescription and calculation model
    let modifyPlan (plan: ExternalPlanSetup) =
        let doseValue = DoseValue(2.0, "Gy")
        plan.SetPrescription(1, doseValue, 1)
        plan.SetCalculationModel(CalculationType.PhotonVolumeDose, "accAcurosXB")
        plan

    // Copy the modified plan to a new structure set
    let copyPlanToNewStructureSet (course: Course) (patient: Patient) (currentPlan: ExternalPlanSetup) =

        // maybe we have to decide where to get structureset
        let structureSetOpt = patient.StructureSets |> Seq.tryHead

        match structureSetOpt with
        | None -> Error "No StructureSet found."
        | Some ss ->
            let newStructureSet = ss.Image.CreateNewStructureSet()

            let searchBodyParameters = ss.GetDefaultSearchBodyParameters()
            newStructureSet.CreateAndSearchBody(searchBodyParameters) |> ignore

            let outputDiagnostics =
                System.Text.StringBuilder("Type in the information about copied plan.")

            let copiedPlan =
                course.CopyPlanSetup(currentPlan, newStructureSet, outputDiagnostics) :?> ExternalPlanSetup

            copiedPlan.Id <- currentPlan.Id + "C"
            Ok copiedPlan

    type VmatBeamParameters =
        {
            ExternalBeamMachioneParameters: ExternalBeamMachineParameters
            MetersetWeights: MetersetValue list
            collimatorAngle
        }

    // Copy the modified plan to a new structure set
    let getExternalBeamMachineParameters (beam : Beam ) : ExternalBeamMachineParameters  =

        let machineId = beam.TreatmentUnit.Id
        let energyModeId = beam.EnergyMode.Id
        let doseRate : int = beam.DoseRate 
        let techniqueId = beam.Technique.Id
        let primaryFluenceModeId = beam.BeamTechnique.ToString()
        let mlcId = beam.MLC
        
        let parameters = new ExternalBeamMachineParameters(
            machineId,
            energyModeId, 
            doseRate, 
            techniqueId, 
            primaryFluenceModeId
        )
            
        parameters

    let getVmatBeamParameters

    let getExternalBeamMachineParametersFromPlan (plan : ExternalPlanSetup) : ExternalBeamMachineParameters list =
        plan.Beams
        |> Seq.map getExternalBeamMachineParameters
        |> Seq.toList

    let addVmatBeams (parameters: ExternalBeamMachineParameters list) (plan: ExternalPlanSetup) =
        parameters
        |> List.iter (fun param ->
            let beam = plan.AddVmatBeam(param)
            beam.SetIsocenterPosition(plan.IsocenterPosition)
            beam.SetBeamName("VMAT Beam")
            beam.SetBeamNumber(1)
        )
        plan)