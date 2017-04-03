/*
Developed by: NumbersBelieve
Function: IBAN validation and display in a row.
Parameters:
	The interaction or entity must have an IBAN attribute.
	Visually, the component should have 10 size - otherwise, you may need to adjust the HTML.
*/

// Define a new JS class
WebComponents.IBANValidator = function(){
	
	// Required method: Used to return the Component's HTML
	this.html = function(){
		return ' 																										\
			<div class="row"> 																			\
				<div class="col-md-2"> 																			\
					<label class="radio-inline">																\
						<input type="radio" name="PaymentType" value="delivery">&nbsp;Cash on Delivery  				\
					</label>																					\
				</div> 																								\
				<div class="col-md-2"> 																			\
					<label class="radio-inline">																\
						<input type="radio" name="PaymentType" value="transfer">&nbsp;Bank Transfer					\
					</label>																					\
				</div> 																								\
			</div> 																								\
			<div class="form-group col row" id="IBAN_Form" style="display: none"> 										\
					<div class="form-inline"> \
						<div class="input-group"><label class="sr-only" for="inlineFormInput">IBAN Form</label></div>						\
						<div class="input-group"><input type="text" class="form-control" name="IBAN_Value" size="4" maxlength="4"></div>				\
						<div class="input-group"><input type="text" class="form-control" name="IBAN_Value" size="4" maxlength="4"></div>				\
						<div class="input-group"><input type="text" class="form-control" name="IBAN_Value" size="4" maxlength="4"></div>				\
						<div class="input-group"><input type="text" class="form-control" name="IBAN_Value" size="4" maxlength="4"></div>				\
						<div class="input-group"><input type="text" class="form-control" name="IBAN_Value" size="4" maxlength="4"></div>				\
						<div class="input-group"><input type="text" class="form-control" name="IBAN_Value" size="4" maxlength="4"></div>				\
						<div class="input-group"><input type="text" class="form-control" name="IBAN_Value" size="1" maxlength="1"></div>				\
					</div> 																					\
			</div> 																									\
		';
	};
	
	// Optional method: Called after the html is added to the page
	// The right place to add custom behaviours
	this.initialize = function (domElement) {
		var ibanField = document.getElementById("IBAN");
		var initialValue = ibanField.value || '';
		
		var radios = document.getElementsByName('PaymentType');
		var inputs = document.getElementsByName('IBAN_Value');
		
		for(var radio in radios) {
			radios[radio].onclick = function() {
				var el = document.getElementById('IBAN_Form');
				if(this.value === 'transfer'){
					el.style.display = '';
				}
				else{
					var ibanField = document.getElementById("IBAN");
					ibanField.value = '';
					ibanField.dispatchEvent(new Event('change'));
					el.style.display = 'none';
				}
			}
			
			if(initialValue !== '' && radios[radio].value === 'transfer'){
				radios[radio].checked = true;
				radios[radio].dispatchEvent(new Event('click'));
			}
		}
		
		for(var input in inputs) {
			if(initialValue !== ''){
				inputs[input].value = initialValue.substring(0,4);
				initialValue = initialValue.substring(4);
			}
			
			inputs[input].onchange = function(event){
				var inputs = document.getElementsByName('IBAN_Value');
				var iban = '';
				for(var input in inputs) {
					iban += inputs[input].value || '';
				}
				
				if (isValidIBANNumber(iban)){
					document.getElementById("IBAN_Form").parentNode.parentNode.classList.remove("has-danger")

					var ibanField = document.getElementById("IBAN");
					ibanField.value = iban;
					ibanField.dispatchEvent(new Event('change'));
				}
				else{					
					document.getElementById("IBAN_Form").parentNode.parentNode.classList.add("has-danger")
				}
			};
		}
	};
	
	//IBAN Validation functions obtained from http://stackoverflow.com/questions/21928083/iban-validation-check/35599724#35599724
	
	/*
	 * Returns 1 if the IBAN is valid 
	 * Returns FALSE if the IBAN's length is not as should be (for CY the IBAN Should be 28 chars long starting with CY )
	 * Returns any other number (checksum) when the IBAN is invalid (check digits do not match)
	 */
	function isValidIBANNumber(input) {
		var CODE_LENGTHS = {
			AD: 24, AE: 23, AT: 20, AZ: 28, BA: 20, BE: 16, BG: 22, BH: 22, BR: 29,
			CH: 21, CR: 21, CY: 28, CZ: 24, DE: 22, DK: 18, DO: 28, EE: 20, ES: 24,
			FI: 18, FO: 18, FR: 27, GB: 22, GI: 23, GL: 18, GR: 27, GT: 28, HR: 21,
			HU: 28, IE: 22, IL: 23, IS: 26, IT: 27, JO: 30, KW: 30, KZ: 20, LB: 28,
			LI: 21, LT: 20, LU: 20, LV: 21, MC: 27, MD: 24, ME: 22, MK: 19, MR: 27,
			MT: 31, MU: 30, NL: 18, NO: 15, PK: 24, PL: 28, PS: 29, PT: 25, QA: 29,
			RO: 24, RS: 22, SA: 24, SE: 24, SI: 19, SK: 24, SM: 27, TN: 24, TR: 26
		};
		var iban = String(input).toUpperCase().replace(/[^A-Z0-9]/g, ''), // keep only alphanumeric characters
				code = iban.match(/^([A-Z]{2})(\d{2})([A-Z\d]+)$/), // match and capture (1) the country code, (2) the check digits, and (3) the rest
				digits;
		// check syntax and length
		if (!code || iban.length !== CODE_LENGTHS[code[1]]) {
			return false;
		}
		// rearrange country code and check digits, and convert chars to ints
		digits = (code[3] + code[1] + code[2]).replace(/[A-Z]/g, function (letter) {
			return letter.charCodeAt(0) - 55;
		});
		// final check
		return mod97(digits);
	}
	
	function mod97(string) {
		var checksum = string.slice(0, 2), fragment;
		for (var offset = 2; offset < string.length; offset += 7) {
			fragment = String(checksum) + string.substring(offset, offset + 7);
			checksum = parseInt(fragment, 10) % 97;
		}
		return checksum;
	}

}