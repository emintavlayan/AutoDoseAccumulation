namespace VMS.TPS

open System
open System.Windows.Forms
open FsToolkit.ErrorHandling
open VMS.TPS.Common.Model.API
open VMS.TPS.Common.Model.Types

module PlanFunctions =

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
        { ExternalBeamMachineParameters: ExternalBeamMachineParameters
          MetersetWeights: float list
          CollimatorAngle: float
          GantryAngle: float
          GantryStop: float
          GantryDirection: GantryDirection
          PatientSupportAngle: float
          IsocenterPosition: VVector }

    // Copy the modified plan to a new structure set
    let getExternalBeamMachineParameters (beam: Beam) : ExternalBeamMachineParameters =

        let machineId = beam.TreatmentUnit.Id
        let energyModeId = beam.EnergyMode.Id
        let doseRate: int = beam.DoseRate
        let techniqueId = beam.Technique.Id
        let primaryFluenceModeId = beam.BeamTechnique.ToString()
        let mlcId = beam.MLC

        let parameters =
            new ExternalBeamMachineParameters(machineId, energyModeId, doseRate, techniqueId, primaryFluenceModeId)

        parameters

    let getVmatBeamParameters (beam: Beam) : VmatBeamParameters =
        // first control point
        let firstControlPoint = beam.ControlPoints |> Seq.head
        let lastControlPoint = beam.ControlPoints |> Seq.last

        { ExternalBeamMachineParameters = getExternalBeamMachineParameters beam
          MetersetWeights = beam.ControlPoints |> Seq.map (fun cp -> cp.MetersetWeight) |> Seq.toList
          CollimatorAngle = firstControlPoint.CollimatorAngle
          GantryAngle = firstControlPoint.GantryAngle
          GantryStop = lastControlPoint.GantryAngle
          GantryDirection = beam.GantryDirection
          PatientSupportAngle = firstControlPoint.PatientSupportAngle
          IsocenterPosition = beam.IsocenterPosition }

    let getVmatBeamParametersFromPlan (plan: ExternalPlanSetup) : VmatBeamParameters list =
        plan.Beams |> Seq.map getVmatBeamParameters |> Seq.toList

    let addVmatBeams (parameters: VmatBeamParameters list) (plan: ExternalPlanSetup) =
        parameters
        |> List.iter (fun p ->
            let beam =
                plan.AddVMATBeam(
                    p.ExternalBeamMachineParameters,
                    p.MetersetWeights,
                    p.CollimatorAngle,
                    p.GantryAngle,
                    p.GantryStop,
                    p.GantryDirection,
                    p.PatientSupportAngle,
                    p.IsocenterPosition
                )

            beam |> ignore)
