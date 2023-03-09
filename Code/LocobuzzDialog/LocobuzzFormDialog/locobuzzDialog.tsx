import * as React from 'react';
import { ContextualMenu, IconButton, IIconProps, IDragOptions, FontWeights, getTheme, mergeStyleSets, IModalStyleProps, IModalStyles, Popup, Layer } from 'office-ui-fabric-react';
import { Modal } from 'office-ui-fabric-react';

import { ILocobuzzDialogComponentProps } from './Classes/ILocobuzzDialogComponentProps';

  const titleId='divDialogtitle';
  const maximizeIcon: IIconProps = { iconName: 'OpenPaneMirrored' };
  const minimizeIcon: IIconProps = { iconName: 'MiniContractMirrored' };
  const mediumSizeIcon: IIconProps = { iconName: 'OpenInNewWindow' };
  
  const theme = getTheme();
  const contentStyles = mergeStyleSets({
    container: {
      display: 'flex',
      flexFlow: 'column nowrap',
      alignItems: 'stretch',
    },
    header: [
      theme.fonts.medium,
      {
        flex: '1 1 auto',
        borderTop: `2px solid ${theme.palette.themePrimary}`,
        color: theme.palette.neutralPrimary,
        display: 'flex',
        alignItems: 'center',
        fontWeight: FontWeights.semibold,
        padding: '3px 5px',
      },
    ],
    body: {
      flex: '4 4 auto',
      padding: '5px',
      overflowY: 'hidden',
      selectors: {
        p: { margin: '14px 0' },
        'p:first-child': { marginTop: 0 },
        'p:last-child': { marginBottom: 0 },
      },
    },
  });

  const iconButtonStyles = {
    root: {
      color: theme.palette.neutralPrimary,
      marginLeft: 'auto',
      marginTop: '4px',
      marginRight: '2px',
    },
    rootHovered: {
      color: theme.palette.neutralDark,
    },
  };

export class  LocobuzzDialog extends React.Component<ILocobuzzDialogComponentProps,any> {
  private containerOffset:number=0;
  private containerWidth:number=0;
  constructor(props: ILocobuzzDialogComponentProps) {
      super(props);
      //this.props.context.resources.getResource("cc_proMX.LocobuzzFormDialog/img/locobuzz_logo_1.png", this.setImage.bind(this, false, "png"), this.showError.bind(this));
      this.containerOffset=this.getOffsetTop(this.props.container,0);
      this.containerWidth=this.props.container.offsetWidth+50;
      this.state = {
        showDialog: false,
        flipDialog: false,
        iframeURL:'',
        width:this.props.container.offsetWidth+50,
        height:(window.innerHeight - this.containerOffset),
        maximizeModel:false,
        imageUrl:"/webresources/cc_proMX.LocobuzzFormDialog/img/locobuzz_logo_1.png"
      };

      var locobuzzId=this.props.context.parameters.locobuzzIdProperty.raw??'';
      if(locobuzzId!==''){
        this.checkUserRoles();
        this.processIframeURL();
        
      }      
    }    

    public render() {
      return (
        <div>
          <Layer  className="layerOverride">
          { this.state.showDialog && this.state.iframeURL!='' && this.state.flipDialog &&(
            <Popup    
              className='ms-dialogSmallOverride'>
              <div className={contentStyles.header}>
                <span id={titleId}><img src={this.state.imageUrl} title='Locobuzz' className='titleLogoSmall'/></span>
                <IconButton
                  styles={iconButtonStyles}
                  iconProps={maximizeIcon}
                  ariaLabel="Restore Right"
                  onClick={this.closeDialog.bind(this)}
                  title="Restore Right"
                />
              </div>
            </Popup>
          )}
          {this.state.showDialog && this.state.iframeURL!='' &&(

            <Popup
              className='ms-dialogMainOverride'
              style={{visibility: (this.state.flipDialog?'hidden':'visible'),
              minWidth:this.state.width,minHeight:this.state.height}}>
            <div className={contentStyles.header}>
              <span id={titleId}><img src={this.state.imageUrl} title='Locobuzz' className='titleLogo'/></span>
              <div style={{display:'inline-block', marginLeft: 'auto'}}>
                {!this.state.maximizeModel &&
                  <IconButton
                    styles={iconButtonStyles}
                    iconProps={mediumSizeIcon}
                    ariaLabel="Maximize"
                    onClick={this.maximizeDialog.bind(this)}
                    title="Maximize"
                  />
                }
                { this.state.maximizeModel &&
                  <IconButton
                    styles={iconButtonStyles}
                    iconProps={maximizeIcon}
                    ariaLabel="Restore Right"
                    onClick={this.closeDialog.bind(this)}
                    title="Restore Right"
                  />
                }
                <IconButton
                  styles={iconButtonStyles}
                  iconProps={minimizeIcon}
                  ariaLabel="Minimize"
                  onClick={this.closeMainDialog.bind(this)}
                  title="Minimize"
                />
              </div>
            </div>
            <div className={contentStyles.body}>
              <iframe src={this.state.iframeURL} sandbox="allow-top-navigation-by-user-activation allow-same-origin allow-scripts allow-downloads allow-popups allow-forms allow-popups-to-escape-sandbox"  width='98%' height={this.state.height-50}></iframe>
            </div>
            </Popup>
          )}
          </Layer>
        </div>
      );
    }
  private setImage(shouldUpdateOutput:boolean, fileType: string, fileContent: string): void
	{
		let imageUrl:string = this.generateImageSrcUrl(fileType, fileContent);
    this.setState({imageUrl:imageUrl});
	}
  private showError(error:any  ): void
	{
	}

	private generateImageSrcUrl(fileType: string, fileContent: string): string
	{
		return  "data:image/" + fileType + ";base64, " + fileContent;
	}
    private maximizeDialog() {
      this.setState( {width: window.innerWidth-100,height:window.innerHeight-50,maximizeModel:true } );
    }

    private closeDialog() {
      this.setState( {flipDialog: false,width:this.containerWidth,height:(window.innerHeight - this.containerOffset),maximizeModel:false } );
    }
    private closeMainDialog() {
      this.setState( {flipDialog: true } );
    }
    private getOffsetTop(element: any,currentHeight:number ):number{
      if(element.offsetParent!=null){
        currentHeight+=element.offsetTop;
        currentHeight=this.getOffsetTop(element.offsetParent,currentHeight);
      }
      return currentHeight;
    }
    private async checkUserRoles(){
      let fetchXML = `<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="true">
                        <entity name="systemuser">
                          <attribute name="systemuserid" />
                          <order attribute="fullname" descending="false" />
                          <filter type="and">
                            <condition attribute="systemuserid" operator="eq" uitype="systemuser" value="${this.props.context.userSettings.userId}" />
                          </filter>
                          <link-entity name="systemuserroles" from="systemuserid" to="systemuserid" visible="false" intersect="true">
                            <link-entity name="role" from="roleid" to="roleid" alias="ac">
                              <filter type="and">
                                <condition attribute="name" operator="eq" value="Locobuzz Basic User" />
                              </filter>
                            </link-entity>
                          </link-entity>
                        </entity>
                      </fetch>`;

                      var userDetails= await this.props.context.webAPI.retrieveMultipleRecords("systemuser", `?fetchXml=${  fetchXML}`);
                      fetchXML = `<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">
                      <entity name="lbz_locobuzzentitymappingconfiguration">
                        <attribute name="lbz_locobuzzentitymappingconfigurationid" />
                        <order attribute="createdon" descending="true" />
                        <filter type="and">
                          <condition attribute="lbz_entitytype" operator="eq" value="851720005" />
                          <condition attribute="statecode" operator="eq" value="0" />
                          <condition attribute="lbz_fieldmapping" operator="like" value="%${this.props.context.userSettings.userId.replace("{","").replace("}","")}%" />
                        </filter>
                      </entity>
                    </fetch>`;

        var userMapping=await this.props.context.webAPI.retrieveMultipleRecords("lbz_locobuzzentitymappingconfiguration", `?fetchXml=${  fetchXML}`);
        if(userDetails.entities.length>0 && userMapping.entities.length>0){
          this.setState( {showDialog: true} );
        }
      }

    private async processIframeURL(){
      var iframeURL='';
      var apiConfig= await this.getAPIConfiguration();
      var entityName= (this.props.context.mode as any).contextInfo.entityTypeName;
      var brandId='';
      var appId='';
      if(apiConfig!=null && apiConfig.entities.length>0){
        if(entityName=='contact'){
          iframeURL=apiConfig.entities[0].lbz_contactiframeurl;
        }
        else if(entityName=='incident'){
          iframeURL=apiConfig.entities[0].lbz_caseiframeurl;
        }
        brandId=apiConfig.entities[0].lbz_brandid;
        appId=apiConfig.entities[0].lbz_appid;
      }

      var tempAccessCode=await this.createAccessCode();
      iframeURL=iframeURL.replace("{AccessCode}",tempAccessCode);
      iframeURL=iframeURL.replace("{LocobuzzId}",this.props.context.parameters.locobuzzIdProperty.raw??"");
      iframeURL=iframeURL.replace("{UserId}",this.props.context.userSettings.userId.replace("{","").replace("}",""));
      iframeURL=iframeURL.replace("{AppId}",appId);
      iframeURL=iframeURL.replace("{Appid}",appId);
      iframeURL=iframeURL.replace("{BrandId}",brandId);
      iframeURL=iframeURL.replace("{Brandid}",brandId);
      this.setState( {iframeURL: iframeURL } );
    }

    private async getAPIConfiguration():Promise<ComponentFramework.WebApi.RetrieveMultipleResponse>{
      let fetchXML = `<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">
                        <entity name="lbz_locobuzzapiconfiguration">
                          <attribute name="lbz_locobuzzapiconfigurationid" />
                          <attribute name="lbz_uniqueidentification" />
                          <attribute name="lbz_contactiframeurl" />
                          <attribute name="lbz_caseiframeurl" />
                          <attribute name="lbz_brandid" />
                          <attribute name="lbz_appid" />
                          <order attribute="createdon" descending="true" />
                          <filter type="and">
                            <condition attribute="statecode" operator="eq" value="0" />
                          </filter>
                        </entity>
                      </fetch>`;

      var apiConfig= await this.props.context.webAPI.retrieveMultipleRecords("lbz_locobuzzapiconfiguration", `?fetchXml=${  fetchXML}`);
      return apiConfig;
    }

    private async createAccessCode():Promise<string>{
      const query = "/api/data/v9.2/lbz_CreateManageAccessCodes";
      const headers = {
          "Accept": "application/json",
          "Content-Type": "application/json; charset=utf-8",
          "OData-MaxVersion": "4.0",
          "OData-Version": "4.0"
      };
      const body = JSON.stringify(
          {
              "UserId": this.props.context.userSettings.userId
          }
      );
      
      var result=await fetch(query, {
          method: 'post',
          headers: headers,
          body: body
      });

      console.log(result);
      var resultText=await result.text();
      return JSON.parse(resultText).ManageAccessCodeId;
    }
  };