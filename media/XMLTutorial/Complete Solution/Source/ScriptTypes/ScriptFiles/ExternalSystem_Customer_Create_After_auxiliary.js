// Modified - 20160616

try {
        
	var connector = new mymis.connector(requestID);
	var context = mymis.helpers.getContextToCompany(contextData, entity.Attributes.Supplier);
	
	if(context== null)
	{
		response.send([{
				StatusCode: "ScriptException",
				StatusMessage: "Missing integration configurations for Company " + entity.Attributes.Supplier,
				Result: ""
			}]);
	}

	connector.onConnectionError = function (err, errMessage) {
		logger.error("onConnectionError " + err, {
			requestID: requestID
		});
		response.send([{
				StatusCode: "ScriptException",
				StatusMessage: errMessage || ((err instanceof Error) ? err.toString() : JSON.stringify(err)),
				Result: ""
			}]);
	};
	
	connector.start(function () {
		var mymis_services = connector.initConnectorServices();
		
		var operations = [];
		for (var i in contextData.Files) {
			if (contextData.Files[i].ExecutionType === "Remote") {
				operations.push({
					Script: contextData.Files[i].Name, 
					ScriptVersion: '' + contextData.Files[i].Version,
					Namespace: "myMIS", 
					Type: "Script", 
					Action: "Execute", 
				});
			}
		}
		
		mymis_services.command(context, operations, entity, function (resp) {
			if (!mymis.helpers.isValidConnectorResponse(resp, response)) {
				connector.close();
			} else {
				connector.close();
				if (resp[0].StatusCode === 'OK') {
					var result = null;
					if (resp[0].Result[0].UpdateEntityProperties != null) {
						result = {
							UpdateEntityProperties: resp[0].Result[0].UpdateEntityProperties
						}
					}
					
					response.send([{
							StatusCode: resp[0].StatusCode,
							StatusMessage: resp[0].Result[0].Message,
							Result: result
						}]);
				}
				else {
					response.send([{
							StatusCode: resp[0].StatusCode,
							StatusMessage: resp[0].StatusMessage,
							Result: null
						}]);
				}
			}
		});
	});

} catch (e) {
	response.send([{
			StatusCode: "ScriptException",
			StatusMessage: e,
			Result: ""
		}]);
}