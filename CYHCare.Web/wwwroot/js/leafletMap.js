// CYH Care — High Performance Leaflet.js + OpenStreetMap Interop Helper
window.cyhMaps = window.cyhMaps || {};

window.cyhMaps.initMap = function (elementId, centerLat, centerLng, zoom) {
    if (!document.getElementById(elementId)) return;

    if (window.cyhMaps[elementId] && window.cyhMaps[elementId].map) {
        window.cyhMaps[elementId].map.remove();
    }

    // High performance hardware accelerated canvas mode
    var map = L.map(elementId, {
        preferCanvas: true,
        zoomControl: true,
        fadeAnimation: true,
        markerZoomAnimation: true
    }).setView([centerLat, centerLng], zoom || 14);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        updateWhenIdle: true,
        updateWhenZooming: false,
        keepBuffer: 3,
        attribution: '&copy; <a href="https://openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    window.cyhMaps[elementId] = {
        map: map,
        markers: {},
        routeLine: null
    };

    setTimeout(function () {
        map.invalidateSize();
    }, 150);
};

window.cyhMaps.addOrUpdateMarker = function (elementId, markerId, lat, lng, popupHtml, isHelper) {
    var instance = window.cyhMaps[elementId];
    if (!instance || !instance.map) return;

    var iconColor = isHelper ? '#0284c7' : '#16a34a';
    var iconHtml = '<div style="background-color:' + iconColor + '; width:22px; height:22px; border-radius:50%; border:3px solid white; box-shadow:0 2px 6px rgba(0,0,0,0.35);"></div>';

    var customIcon = L.divIcon({
        className: 'cyh-custom-marker',
        html: iconHtml,
        iconSize: [22, 22],
        iconAnchor: [11, 11]
    });

    if (instance.markers[markerId]) {
        instance.markers[markerId].setLatLng([lat, lng]);
        if (popupHtml) instance.markers[markerId].bindPopup(popupHtml);
    } else {
        var marker = L.marker([lat, lng], { icon: customIcon }).addTo(instance.map);
        if (popupHtml) marker.bindPopup(popupHtml);
        instance.markers[markerId] = marker;
    }
};

window.cyhMaps.centerMap = function (elementId, lat, lng, zoom) {
    var instance = window.cyhMaps[elementId];
    if (instance && instance.map) {
        instance.map.setView([lat, lng], zoom || instance.map.getZoom(), { animate: true });
    }
};

window.cyhMaps.enableClickPicker = function (elementId, dotNetHelper) {
    var instance = window.cyhMaps[elementId];
    if (!instance || !instance.map) return;

    instance.map.on('click', function (e) {
        var lat = e.latlng.lat;
        var lng = e.latlng.lng;

        window.cyhMaps.addOrUpdateMarker(elementId, 'picked_target', lat, lng, 'Target Location', false);
        dotNetHelper.invokeMethodAsync('OnLocationPicked', lat, lng);
    });
};
