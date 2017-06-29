/*
Developed by: NumbersBelieve
Function: Display an embedded Google Maps map, and have it respond to changes in our model. Defaults to showing Portugal if no address is in the field.
Parameters:
	Replace @@APIKEY@@ with a free Google Maps API Key. See https://developers.google.com/maps/documentation/embed/guide
	Use in an interaction or entity that has a ShippingAddress field.
*/

WebComponents.Map = function () {

	this.html = function () {
		return '<iframe id="ShippingAddressMap" width="100%" height="450" frameborder="0" style="border:0" allowfullscreen src="https://www.google.com/maps/embed/v1/place?key=@@APIKEY@@&zoom=5&q=Portugal" ></iframe>';
	};

	var lastValue = null;
	var timer = null;
	this.initialize = function (domElement) {
		renderMap(domElement);

		addEventListenerToElement('ShippingAddress', domElement);

		timer = setInterval(function () { checkForChanges(domElement) }, 1000);
	};

	var addEventListenerToElement = function (elementID, domElement) {
		document.getElementById(elementID).addEventListener('change', function (event) {
			renderMap(domElement);
		});
	}

	var getValueFromElement = function (elementID) {
		return document.getElementById(elementID).value || '';
	}

	var renderMap = function (domElement) {
		var address = getValueFromElement('ShippingAddress');

		lastValue = address;

		domElement.innerHTML = '';

		var map = document.createElement('iframe');
		map.id = 'ShippingAddressMap';
		map.setAttribute('width', '100%');
		map.setAttribute('height', '450');
		map.setAttribute('frameborder', '0');
		map.setAttribute('style', 'border:0');
		map.setAttribute('allowfullscreen', '');

		if (address !== '') {
			address = platform.utils.text.replaceAll(address, { ', ': ',', ' ': '+' });
			map.src = 'https://www.google.com/maps/embed/v1/place?key=@@APIKEY@@&zoom=16&q=' + address;
		}
		else {
			map.src = 'https://www.google.com/maps/embed/v1/place?key=@@APIKEY@@&zoom=5&q=Portugal';
		}

		domElement.appendChild(map);
	}

	var checkForChanges = function (domElement) {
		if (lastValue !== getValueFromElement('ShippingAddress')){
			renderMap(domElement);
			clearInterval(timer);
		}
	}
}
