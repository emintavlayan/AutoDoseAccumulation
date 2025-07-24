### Auto Dose Accumulation ###
Esapi code with beautiful FSharp
# Auto Dose Accumulation

This project demonstrates how to automate basic plan copying and dose accumulation
using the Varian ESAPI API with F#. The library builds against .NET Framework 4.8
and references the ESAPI assemblies installed with Eclipse 18.

## Building
1. Ensure the machine has .NET Framework 4.8 installed.
2. Open `autoDoseAccumulation.sln` in Visual Studio.
3. Fix the reference paths to `VMS.TPS.Common.Model.*` assemblies if needed so they
   point to your local Eclipse installation.
4. Build the solution in 64‑bit mode.

## Running
The compiled assembly is an ESAPI script. Deploy the resulting `*.esapi.dll` file
according to your clinic's ESAPI scripting guidelines and run it from Eclipse.

