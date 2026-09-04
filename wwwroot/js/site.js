// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

const mapaCoordenadas = document.getElementById('mapaCoordenadas');
if (mapaCoordenadas && window.L) {
    const inputCoordenadas = document.getElementById('Coordenadas');
    const inputLinkOsm = document.getElementById('osmLink');
    const coordenadasIniciales = parsearCoordenadas(inputCoordenadas?.value || mapaCoordenadas.dataset.coordenadas);
    const centroInicial = coordenadasIniciales || [-40.63, -63.28];
    const zoomInicial = coordenadasIniciales ? 15 : 5;

    const mapa = L.map('mapaCoordenadas').setView(centroInicial, zoomInicial);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(mapa);

    let marcador = null;

    function fijarCoordenadas(lat, lng, moverMapa) {
        const latitud = Number(lat);
        const longitud = Number(lng);
        if (!Number.isFinite(latitud) || !Number.isFinite(longitud) || latitud < -90 || latitud > 90 || longitud < -180 || longitud > 180) {
            return;
        }

        const punto = [latitud, longitud];
        if (!marcador) {
            marcador = L.marker(punto).addTo(mapa);
        } else {
            marcador.setLatLng(punto);
        }

        if (inputCoordenadas) {
            inputCoordenadas.value = `${latitud.toFixed(6)},${longitud.toFixed(6)}`;
            inputCoordenadas.dispatchEvent(new Event('input', { bubbles: true }));
        }

        if (moverMapa) {
            mapa.setView(punto, Math.max(mapa.getZoom(), 15));
        }
    }

    if (coordenadasIniciales) {
        fijarCoordenadas(coordenadasIniciales[0], coordenadasIniciales[1], false);
    }

    mapa.on('click', event => {
        fijarCoordenadas(event.latlng.lat, event.latlng.lng, false);
    });

    inputCoordenadas?.addEventListener('change', () => {
        const coordenadas = parsearCoordenadas(inputCoordenadas.value);
        if (coordenadas) {
            fijarCoordenadas(coordenadas[0], coordenadas[1], true);
        }
    });

    inputLinkOsm?.addEventListener('change', () => {
        const coordenadas = parsearLinkOpenStreetMap(inputLinkOsm.value);
        if (coordenadas) {
            fijarCoordenadas(coordenadas[0], coordenadas[1], true);
        }
    });
}

function parsearCoordenadas(valor) {
    const match = (valor || '').trim().match(/^(-?\d{1,2}(?:\.\d+)?)\s*,\s*(-?\d{1,3}(?:\.\d+)?)$/);
    if (!match) {
        return null;
    }

    const lat = Number(match[1]);
    const lng = Number(match[2]);
    return lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180 ? [lat, lng] : null;
}

function parsearLinkOpenStreetMap(valor) {
    const match = (valor || '').match(/#map=\d+\/(-?\d+(?:\.\d+)?)\/(-?\d+(?:\.\d+)?)/);
    if (!match) {
        return null;
    }

    const lat = Number(match[1]);
    const lng = Number(match[2]);
    return lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180 ? [lat, lng] : null;
}

document.querySelectorAll('.js-select-busqueda').forEach(input => {
    let timeoutId;
    input.addEventListener('input', () => {
        clearTimeout(timeoutId);
        timeoutId = setTimeout(async () => {
            const select = document.getElementById(input.dataset.target);
            if (!select) {
                return;
            }

            const url = new URL(input.dataset.url, window.location.origin);
            if (input.value.trim()) {
                url.searchParams.set('q', input.value.trim());
            }

            if (input.dataset.fechaInicio) {
                const fechaInicio = document.getElementById(input.dataset.fechaInicio)?.value;
                const fechaFin = document.getElementById(input.dataset.fechaFin)?.value;
                const reservaId = input.dataset.reservaId;
                if (fechaInicio) {
                    url.searchParams.set('fechaInicio', fechaInicio);
                }
                if (fechaFin) {
                    url.searchParams.set('fechaFin', fechaFin);
                }
                if (select.value && select.value !== '0') {
                    url.searchParams.set('inmuebleSeleccionadoId', select.value);
                }
                if (reservaId && reservaId !== '0') {
                    url.searchParams.set('reservaId', reservaId);
                }
            }

            const valorActual = select.value;
            const response = await fetch(url);
            if (!response.ok) {
                return;
            }

            const opciones = await response.json();
            const primeraOpcion = select.querySelector('option[value="0"]')?.textContent || 'Seleccione una opcion';
            select.innerHTML = `<option value="0">${primeraOpcion}</option>`;

            opciones.forEach(opcion => {
                const option = document.createElement('option');
                option.value = opcion.id;
                option.textContent = opcion.texto;
                if (String(opcion.id) === valorActual) {
                    option.selected = true;
                }
                select.appendChild(option);
            });
        }, 300);
    });
});
