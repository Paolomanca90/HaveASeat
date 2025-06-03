window.closeModal = function () {
    const modal = document.getElementById('promotionModal');
    if (modal) {
        modal.remove();
        console.log('Modal rimosso');
    }
};

window.disablePromotion = function () {
    if (typeof Swal !== 'undefined') {
        Swal.fire({
            title: 'Disattivare la promozione?',
            text: 'Il servizio tornerà al prezzo standard.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Disattiva',
            cancelButtonText: 'Annulla',
            confirmButtonColor: '#ef4444',
            cancelButtonColor: '#6b7280'
        }).then((result) => {
            if (result.isConfirmed) {
                window.submitTogglePromotion(false, null, null);
            }
        });
    } else {
        if (confirm('Disattivare la promozione?')) {
            window.submitTogglePromotion(false, null, null);
        }
    }
};

window.savePromotion = function () {
    const prezzoPromozione = parseFloat(document.getElementById('prezzoPromozione').value);
    const dataFinePromozione = document.getElementById('dataFinePromozione').value;

    // Validazione
    if (!prezzoPromozione || prezzoPromozione <= 0) {
        if (typeof Swal !== 'undefined') {
            Swal.fire('Errore', 'Inserisci un prezzo promozionale valido', 'error');
        } else {
            alert('Inserisci un prezzo promozionale valido');
        }
        return;
    }

    if (!dataFinePromozione) {
        if (typeof Swal !== 'undefined') {
            Swal.fire('Errore', 'Seleziona una data di fine promozione', 'error');
        } else {
            alert('Seleziona una data di fine promozione');
        }
        return;
    }

    if (new Date(dataFinePromozione) <= new Date()) {
        if (typeof Swal !== 'undefined') {
            Swal.fire('Errore', 'La data di fine deve essere futura', 'error');
        } else {
            alert('La data di fine deve essere futura');
        }
        return;
    }

    window.submitTogglePromotion(true, prezzoPromozione, dataFinePromozione);
};

window.submitTogglePromotion = function (isPromotion, prezzoPromozione, dataFinePromozione) {
    const servizioId = document.getElementById('servizioId').value;

    // Cerca il token antiforgery
    let token = null;
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    if (tokenInput) {
        token = tokenInput.value;
    } else {
        console.log('Token antiforgery non trovato');
    }

    const requestData = {
        ServizioId: servizioId,
        IsPromotion: isPromotion,
        PrezzoPromozione: prezzoPromozione,
        DataFinePromozione: dataFinePromozione
    };

    if (typeof Swal !== 'undefined') {
        Swal.fire({
            title: isPromotion ? 'Attivazione in corso...' : 'Disattivazione in corso...',
            allowOutsideClick: false,
            showConfirmButton: false,
            willOpen: () => {
                Swal.showLoading();
            }
        });
    }

    const headers = {
        'Content-Type': 'application/json'
    };

    if (token) {
        headers['RequestVerificationToken'] = token;
    }

    fetch('/Servizio/TogglePromotion', {
        method: 'POST',
        headers: headers,
        body: JSON.stringify(requestData)
    })
        .then(response => {
            return response.text().then(text => {
                try {
                    return JSON.parse(text);
                } catch (e) {
                    throw new Error('Risposta non è JSON valido: ' + text);
                }
            });
        })
        .then(data => {

            if (data.success) {
                if (typeof Swal !== 'undefined') {
                    Swal.fire({
                        icon: 'success',
                        title: 'Successo!',
                        text: data.message || 'Operazione completata con successo',
                        confirmButtonColor: '#3B82F6'
                    }).then(() => {
                        window.closeModal();
                        location.reload();
                    });
                } else {
                    alert('Operazione completata con successo!');
                    window.closeModal();
                    location.reload();
                }
            } else {
                if (typeof Swal !== 'undefined') {
                    Swal.fire('Errore', data.message || 'Errore durante l\'operazione.', 'error');
                } else {
                    alert('Errore: ' + (data.message || 'Errore durante l\'operazione.'));
                }
            }
        })
        .catch(error => {
            if (typeof Swal !== 'undefined') {
                Swal.fire('Errore', 'Si è verificato un errore: ' + error.message, 'error');
            } else {
                alert('Errore: ' + error.message);
            }
        });
};

function dismissAlerts() {
    setTimeout(function () {
        const alerts = document.querySelectorAll('.alert');
        alerts.forEach(alert => {
            alert.classList.add('opacity-0', '-translate-y-4');
            setTimeout(() => alert.remove(), 300);
        });
    }, 5000);
}

// Inizializzazione
document.addEventListener('DOMContentLoaded', function () {
    dismissAlerts();
});