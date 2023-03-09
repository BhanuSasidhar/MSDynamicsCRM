import { IInputs } from "../generated/ManifestTypes";


export interface ILocobuzzDialogComponentProps {
    context: ComponentFramework.Context<IInputs>;
    container:HTMLDivElement;
  }