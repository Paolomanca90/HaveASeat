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

/**
 * Gestione Promozioni - JavaScript
 * Sistema completo per la gestione delle promozioni servizi
 */

// Variabili globali
let currentSaloneId = null;
let availableServices = [];
let activePromotions = [];

// === FUNZIONI PRINCIPALI ===

/**
 * Inizializza il sistema promozioni
 */
function initPromotions(saloneId, services = []) {
    currentSaloneId = saloneId;
    availableServices = services;

    // Inizializza event listeners
    setupEventListeners();

    // Carica promozioni attive
    loadActivePromotions();

    console.log('Sistema promozioni inizializzato', { saloneId, servicesCount: services.length });
}

/**
 * Setup event listeners
 */
function setupEventListeners() {
    // ESC key per chiudere modal
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeAllModals();
        }
    });

    // Click fuori dai modal per chiuderli
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('modal-backdrop')) {
            closeAllModals();
        }
    });
}

// === GESTIONE MODAL ===

/**
 * Chiude tutti i modal aperti
 */
function closeAllModals() {
    const modals = document.querySelectorAll('.fixed.inset-0');
    modals.forEach(modal => {
        if (!modal.classList.contains('hidden')) {
            modal.classList.add('hidden');
        }
    });
}

/**
 * Apre il modal per singola promozione
 */
function togglePromotion(servizioId) {
    if (!servizioId) {
        showError('ID servizio non valido');
        return;
    }

    // Mostra loading
    showLoading('Caricamento modal...');

    fetch(`/Servizio/GetPromotionModal/${servizioId}`)
        .then(response => {
            hideLoading();

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            return response.text();
        })
        .then(html => {
            // Rimuovi eventuali modal esistenti
            const existingModal = document.getElementById('promotionModal');
            if (existingModal) {
                existingModal.remove();
            }

            // Aggiungi il nuovo modal
            document.body.insertAdjacentHTML('beforeend', html);

            // Verifica che il modal sia stato aggiunto
            const newModal = document.getElementById('promotionModal');
            if (newModal) {
                // Setup event listeners per il modal
                setupPromotionModalListeners(newModal);

                // Focus sul primo input
                const firstInput = newModal.querySelector('input[type="number"]');
                if (firstInput) {
                    setTimeout(() => firstInput.focus(), 100);
                }
            }
        })
        .catch(error => {
            hideLoading();
            console.error('Errore caricamento modal:', error);
            showError('Impossibile caricare il modal: ' + error.message);
        });
}

/**
 * Setup listeners per il modal promozione singola
 */
function setupPromotionModalListeners(modal) {
    // Click outside to close
    modal.addEventListener('click', function (e) {
        if (e.target === this) {
            window.closeModal();
        }
    });

    // Validazione real-time
    const prezzoInput = modal.querySelector('#prezzoPromozione');
    const dataInput = modal.querySelector('#dataFinePromozione');

    if (prezzoInput) {
        prezzoInput.addEventListener('input', validatePromotionPrice);
    }

    if (dataInput) {
        dataInput.addEventListener('change', validatePromotionDate);
    }
}

/**
 * Salva promozione singola
 */
function savePromotion() {
    const modal = document.getElementById('promotionModal');
    if (!modal) return;

    const servizioId = modal.querySelector('#servizioId').value;
    const prezzoPromozione = modal.querySelector('#prezzoPromozione').value;
    const dataFinePromozione = modal.querySelector('#dataFinePromozione').value;

    // Validazione
    if (!validatePromotionData(prezzoPromozione, dataFinePromozione)) {
        return;
    }

    // Mostra loading
    showLoading('Salvando promozione...');

    const requestData = {
        servizioId: servizioId,
        isPromotion: true,
        prezzoPromozione: parseFloat(prezzoPromozione),
        dataFinePromozione: dataFinePromozione
    };

    fetch('/Servizio/TogglePromotion', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: JSON.stringify(requestData)
    })
        .then(response => response.json())
        .then(data => {
            hideLoading();

            if (data.success) {
                closeAllModals();
                showSuccess(data.message || 'Promozione attivata con successo!');

                // Aggiorna la pagina dopo un breve delay
                setTimeout(() => {
                    location.reload();
                }, 1500);
            } else {
                showError(data.message || 'Errore durante il salvataggio');
            }
        })
        .catch(error => {
            hideLoading();
            console.error('Errore salvataggio promozione:', error);
            showError('Errore di rete durante il salvataggio');
        });
}

/**
 * Disattiva promozione
 */
function disablePromotion() {
    const modal = document.getElementById('promotionModal');
    if (!modal) return;

    const servizioId = modal.querySelector('#servizioId').value;

    Swal.fire({
        title: '⏹️ Disattivare Promozione?',
        text: 'Il servizio tornerà al prezzo standard',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Disattiva',
        cancelButtonText: 'Annulla',
        confirmButtonColor: '#dc2626'
    }).then((result) => {
        if (result.isConfirmed) {
            executeDisablePromotion(servizioId);
        }
    });
}

/**
 * Esegue la disattivazione della promozione
 */
function executeDisablePromotion(servizioId) {
    showLoading('Disattivando promozione...');

    const requestData = {
        servizioId: servizioId,
        isPromotion: false
    };

    fetch('/Servizio/TogglePromotion', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: JSON.stringify(requestData)
    })
        .then(response => response.json())
        .then(data => {
            hideLoading();

            if (data.success) {
                closeAllModals();
                showSuccess('Promozione disattivata con successo!');

                setTimeout(() => {
                    location.reload();
                }, 1500);
            } else {
                showError(data.message || 'Errore durante la disattivazione');
            }
        })
        .catch(error => {
            hideLoading();
            console.error('Errore disattivazione promozione:', error);
            showError('Errore di rete durante la disattivazione');
        });
}

// === GESTIONE PROMOZIONI MULTIPLE ===

/**
 * Apre il modal per promozioni multiple
 */
//function openBulkPromotionModal() {
//    if (!currentSaloneId) {
//        showError('Nessun salone selezionato');
//        return;
//    }

//    // Carica servizi disponibili
//    loadAvailableServices()
//        .then(services => {
//            // Usa la funzione globale definita nel modal
//            if (typeof window.openBulkPromotionModal === 'function') {
//                window.openBulkPromotionModal(currentSaloneId, services);
//            } else {
//                showBulkModal(services);
//            }
//        })
//        .catch(error => {
//            console.error('Errore caricamento servizi:', error);
//            showError('Impossibile caricare i servizi disponibili');
//        });
//}

/**
 * Carica servizi disponibili per promozioni
 */
function loadAvailableServices() {
    return fetch(`/Promotions/GetAvailableServices?saloneId=${currentSaloneId}`)
        .then(response => {
            if (!response.ok) {
                throw new Error('Errore nel caricamento servizi');
            }
            return response.json();
        })
        .catch(() => {
            // Fallback: usa i servizi già caricati filtrando quelli senza promozione
            return availableServices.filter(s => !s.isPromotion);
        });
}

/**
 * Mostra il modal bulk (fallback se la funzione globale non è disponibile)
 */
function showBulkModal(services) {
    const modal = document.getElementById('bulkPromotionModal');
    if (modal) {
        document.getElementById('bulkSaloneId').value = currentSaloneId;
        populateServicesInModal(services);
        modal.classList.remove('hidden');
    }
}

// === VALIDAZIONE ===

/**
 * Valida i dati della promozione
 */
function validatePromotionData(prezzo, dataFine) {
    if (!prezzo || isNaN(prezzo) || parseFloat(prezzo) <= 0) {
        showError('Inserisci un prezzo promozionale valido');
        return false;
    }

    if (!dataFine) {
        showError('Seleziona una data di fine promozione');
        return false;
    }

    const dataFineDate = new Date(dataFine);
    const oggi = new Date();
    oggi.setHours(0, 0, 0, 0);

    if (dataFineDate <= oggi) {
        showError('La data di fine deve essere futura');
        return false;
    }

    return true;
}

/**
 * Validazione real-time prezzo
 */
function validatePromotionPrice(event) {
    const input = event.target;
    const value = parseFloat(input.value);
    const max = parseFloat(input.max);

    // Rimuovi stili di errore precedenti
    input.classList.remove('border-red-500', 'ring-2', 'ring-red-200');

    if (isNaN(value) || value <= 0) {
        input.classList.add('border-red-500', 'ring-2', 'ring-red-200');
        return false;
    }

    if (value >= max) {
        input.classList.add('border-red-500', 'ring-2', 'ring-red-200');
        showError(`Il prezzo deve essere inferiore a €${max.toFixed(2)}`);
        return false;
    }

    return true;
}

/**
 * Validazione real-time data
 */
function validatePromotionDate(event) {
    const input = event.target;
    const dataSelezionata = new Date(input.value);
    const oggi = new Date();

    // Rimuovi stili di errore precedenti
    input.classList.remove('border-red-500', 'ring-2', 'ring-red-200');

    if (dataSelezionata <= oggi) {
        input.classList.add('border-red-500', 'ring-2', 'ring-red-200');
        showError('La data deve essere futura');
        return false;
    }

    return true;
}

// === FUNZIONI DI UTILITÀ ===

/**
 * Recupera il token CSRF
 */
function getAntiForgeryToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    return token ? token.value : '';
}

/**
 * Carica promozioni attive
 */
function loadActivePromotions() {
    if (!currentSaloneId) return;

    fetch(`/Promotions/GetActivePromotions?saloneId=${currentSaloneId}`)
        .then(response => response.json())
        .then(data => {
            activePromotions = data || [];
            updatePromotionsUI();
        })
        .catch(error => {
            console.error('Errore caricamento promozioni attive:', error);
        });
}

/**
 * Aggiorna UI delle promozioni
 */
function updatePromotionsUI() {
    // Aggiorna badge e indicatori
    const promoBadges = document.querySelectorAll('.promo-badge');
    promoBadges.forEach(badge => {
        const servizioId = badge.dataset.servizioId;
        const hasActivePromo = activePromotions.some(p => p.servizioId === servizioId);

        if (hasActivePromo) {
            badge.classList.remove('hidden');
        } else {
            badge.classList.add('hidden');
        }
    });
}

// === NOTIFICHE ===

let loadingToast = null;

/**
 * Mostra loading toast
 */
function showLoading(message = 'Caricamento...') {
    hideLoading(); // Rimuovi eventuali loading precedenti

    if (typeof Swal !== 'undefined') {
        loadingToast = Swal.fire({
            title: message,
            allowOutsideClick: false,
            allowEscapeKey: false,
            showConfirmButton: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });
    }
}

/**
 * Nasconde loading toast
 */
function hideLoading() {
    if (loadingToast) {
        loadingToast.close();
        loadingToast = null;
    }
}

/**
 * Mostra messaggio di successo
 */
function showSuccess(message, options = {}) {
    if (typeof Swal !== 'undefined') {
        Swal.fire({
            icon: 'success',
            title: '✅ Operazione completata!',
            text: message,
            confirmButtonColor: '#10b981',
            timer: options.timer || 3000,
            timerProgressBar: true,
            ...options
        });
    } else {
        alert('Successo: ' + message);
    }
}

/**
 * Mostra messaggio di errore
 */
function showError(message, options = {}) {
    if (typeof Swal !== 'undefined') {
        Swal.fire({
            icon: 'error',
            title: '❌ Errore',
            text: message,
            confirmButtonColor: '#ef4444',
            ...options
        });
    } else {
        alert('Errore: ' + message);
    }
}

/**
 * Mostra notifica toast
 */
function showToast(message, type = 'info', duration = 3000) {
    if (typeof Swal !== 'undefined') {
        const icons = {
            success: 'success',
            error: 'error',
            warning: 'warning',
            info: 'info'
        };

        Swal.fire({
            toast: true,
            position: 'top-end',
            icon: icons[type] || 'info',
            title: message,
            showConfirmButton: false,
            timer: duration,
            timerProgressBar: true,
            didOpen: (toast) => {
                toast.addEventListener('mouseenter', Swal.stopTimer);
                toast.addEventListener('mouseleave', Swal.resumeTimer);
            }
        });
    }
}

// === FUNZIONI GLOBALI PER COMPATIBILITÀ ===

/**
 * Chiude il modal promozione (compatibilità)
 */
window.closeModal = function () {
    const modal = document.getElementById('promotionModal');
    if (modal) {
        modal.remove();
    }
};

/**
 * Salva promozione (compatibilità)
 */
window.savePromotion = savePromotion;

/**
 * Disattiva promozione (compatibilità)
 */
window.disablePromotion = disablePromotion;

/**
 * Toggle promozione (compatibilità)
 */
window.togglePromotion = togglePromotion;

// === ESTENSIONI PER GESTIONE AVANZATA ===

/**
 * Estende promozione esistente
 */
function extendPromotion(servizioId, nuovaDataFine) {
    if (!servizioId || !nuovaDataFine) {
        showError('Parametri non validi');
        return;
    }

    showLoading('Estendendo promozione...');

    fetch('/Promotions/ExtendPromotion', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: `servizioId=${servizioId}&nuovaDataFine=${nuovaDataFine}`
    })
        .then(response => response.json())
        .then(data => {
            hideLoading();

            if (data.success) {
                showSuccess(data.message);
                setTimeout(() => location.reload(), 1500);
            } else {
                showError(data.message);
            }
        })
        .catch(error => {
            hideLoading();
            console.error('Errore estensione promozione:', error);
            showError('Errore durante l\'estensione della promozione');
        });
}

/**
 * Duplica promozione esistente
 */
function duplicatePromotion(servizioId, targetServizioId) {
    if (!servizioId || !targetServizioId) {
        showError('Parametri non validi per la duplicazione');
        return;
    }

    showLoading('Duplicando promozione...');

    fetch('/Promotions/DuplicatePromotion', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: JSON.stringify({
            sourceServizioId: servizioId,
            targetServizioId: targetServizioId
        })
    })
        .then(response => response.json())
        .then(data => {
            hideLoading();

            if (data.success) {
                showSuccess(data.message);
                setTimeout(() => location.reload(), 1500);
            } else {
                showError(data.message);
            }
        })
        .catch(error => {
            hideLoading();
            console.error('Errore duplicazione promozione:', error);
            showError('Errore durante la duplicazione della promozione');
        });
}

/**
 * Rimuove promozione scaduta
 */
function removeExpiredPromotion(servizioId) {
    Swal.fire({
        title: '🗑️ Rimuovere Promozione Scaduta?',
        text: 'La promozione verrà rimossa definitivamente',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Rimuovi',
        cancelButtonText: 'Annulla',
        confirmButtonColor: '#ef4444'
    }).then((result) => {
        if (result.isConfirmed) {
            executeDisablePromotion(servizioId);
        }
    });
}

/**
 * Rinnova promozione scaduta
 */
function renewPromotion(servizioId) {
    // Simula il click su toggle per aprire il modal di gestione
    togglePromotion(servizioId);
}

/**
 * Rinnova tutte le promozioni scadute
 */
function renewAllExpired(saloneId, expiredPromotions) {
    if (!expiredPromotions || expiredPromotions.length === 0) {
        showToast('Non ci sono promozioni scadute da rinnovare', 'info');
        return;
    }

    Swal.fire({
        title: '🔄 Rinnova Tutte le Promozioni',
        html: `
            <div class="text-left space-y-4">
                <p>Rinnova tutte le ${expiredPromotions.length} promozioni scadute:</p>
                <div>
                    <label class="block text-sm font-medium text-gray-700 mb-2">Nuova Data Fine</label>
                    <input type="date" 
                           id="renewDataFine" 
                           min="${new Date().toISOString().split('T')[0]}" 
                           value="${new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]}" 
                           class="w-full px-3 py-2 border border-gray-300 rounded-md">
                </div>
                <div>
                    <label class="block text-sm font-medium text-gray-700 mb-2">Nuovo Sconto (%)</label>
                    <input type="number" 
                           id="renewDiscount" 
                           min="1" 
                           max="99" 
                           value="20" 
                           class="w-full px-3 py-2 border border-gray-300 rounded-md">
                </div>
            </div>
        `,
        showCancelButton: true,
        confirmButtonText: 'Rinnova Tutte',
        cancelButtonText: 'Annulla',
        confirmButtonColor: '#059669',
        preConfirm: () => {
            const dataFine = document.getElementById('renewDataFine').value;
            const sconto = document.getElementById('renewDiscount').value;

            if (!dataFine || new Date(dataFine) <= new Date()) {
                Swal.showValidationMessage('Seleziona una data futura');
                return false;
            }

            if (!sconto || sconto < 1 || sconto > 99) {
                Swal.showValidationMessage('Inserisci uno sconto valido (1-99%)');
                return false;
            }

            return { dataFine, sconto };
        }
    }).then((result) => {
        if (result.isConfirmed) {
            executeRenewAll(saloneId, expiredPromotions, result.value);
        }
    });
}

/**
 * Esegue il rinnovo di tutte le promozioni
 */
function executeRenewAll(saloneId, expiredPromotions, options) {
    showLoading('Rinnovando tutte le promozioni...');

    const promises = expiredPromotions.map(promo => {
        const newPrice = promo.prezzo * (1 - options.sconto / 100);

        return fetch('/Servizio/TogglePromotion', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({
                servizioId: promo.servizioId,
                isPromotion: true,
                prezzoPromozione: newPrice,
                dataFinePromozione: options.dataFine
            })
        }).then(response => response.json());
    });

    Promise.allSettled(promises)
        .then(results => {
            hideLoading();

            const successes = results.filter(r => r.status === 'fulfilled' && r.value.success).length;
            const failures = results.length - successes;

            if (successes > 0) {
                showSuccess(`${successes} promozioni rinnovate con successo!`);
                if (failures > 0) {
                    showToast(`${failures} promozioni non sono state rinnovate`, 'warning');
                }
                setTimeout(() => location.reload(), 2000);
            } else {
                showError('Errore nel rinnovo delle promozioni');
            }
        })
        .catch(error => {
            hideLoading();
            console.error('Errore rinnovo multiplo:', error);
            showError('Errore durante il rinnovo delle promozioni');
        });
}

// === GESTIONE ANALYTICS E REPORT ===

/**
 * Genera report delle promozioni
 */
function generatePromotionReport(saloneId, dateRange = 'month') {
    showLoading('Generando report...');

    fetch(`/Promotions/GenerateReport?saloneId=${saloneId}&range=${dateRange}`)
        .then(response => {
            hideLoading();

            if (response.ok) {
                return response.blob();
            } else {
                throw new Error('Errore nella generazione del report');
            }
        })
        .then(blob => {
            // Download del file
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `report-promozioni-${dateRange}-${new Date().toISOString().split('T')[0]}.pdf`;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);

            showSuccess('Report generato e scaricato!');
        })
        .catch(error => {
            hideLoading();
            console.error('Errore generazione report:', error);
            showError('Errore durante la generazione del report');
        });
}

/**
 * Esporta dati promozioni
 */
function exportPromotionsData(saloneId, format = 'excel') {
    const formats = {
        excel: { url: '/Promotions/ExportExcel', filename: 'promozioni.xlsx' },
        csv: { url: '/Promotions/ExportCSV', filename: 'promozioni.csv' },
        json: { url: '/Promotions/ExportJSON', filename: 'promozioni.json' }
    };

    const selectedFormat = formats[format];
    if (!selectedFormat) {
        showError('Formato non supportato');
        return;
    }

    showLoading('Esportando dati...');

    fetch(`${selectedFormat.url}?saloneId=${saloneId}`)
        .then(response => {
            hideLoading();

            if (response.ok) {
                return response.blob();
            } else {
                throw new Error('Errore nell\'esportazione');
            }
        })
        .then(blob => {
            // Download del file
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = selectedFormat.filename;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);

            showSuccess('Dati esportati con successo!');
        })
        .catch(error => {
            hideLoading();
            console.error('Errore esportazione:', error);
            showError('Errore durante l\'esportazione dei dati');
        });
}

// === FUNZIONI DI MONITORAGGIO ===

/**
 * Monitora performance promozioni
 */
function monitorPromotionPerformance(servizioId) {
    fetch(`/Promotions/GetPerformanceData?servizioId=${servizioId}`)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showPromotionPerformanceModal(data.data);
            } else {
                showError('Dati non disponibili');
            }
        })
        .catch(error => {
            console.error('Errore caricamento performance:', error);
            showError('Errore nel caricamento dei dati di performance');
        });
}

/**
 * Mostra modal con performance promozione
 */
function showPromotionPerformanceModal(data) {
    Swal.fire({
        title: '📊 Performance Promozione',
        html: `
            <div class="text-left space-y-4">
                <div class="grid grid-cols-2 gap-4">
                    <div class="bg-green-50 p-3 rounded">
                        <div class="text-sm text-gray-600">Prenotazioni</div>
                        <div class="text-xl font-bold text-green-600">${data.bookings || 0}</div>
                    </div>
                    <div class="bg-blue-50 p-3 rounded">
                        <div class="text-sm text-gray-600">Ricavi</div>
                        <div class="text-xl font-bold text-blue-600">€${(data.revenue || 0).toFixed(2)}</div>
                    </div>
                    <div class="bg-orange-50 p-3 rounded">
                        <div class="text-sm text-gray-600">Incremento</div>
                        <div class="text-xl font-bold text-orange-600">${(data.increase || 0).toFixed(1)}%</div>
                    </div>
                    <div class="bg-purple-50 p-3 rounded">
                        <div class="text-sm text-gray-600">Valutazione</div>
                        <div class="text-xl font-bold text-purple-600">${data.rating || 0}/5</div>
                    </div>
                </div>
                <div class="text-sm text-gray-600">
                    <strong>Periodo:</strong> ${data.period || 'N/A'}
                </div>
            </div>
        `,
        confirmButtonText: 'Chiudi',
        confirmButtonColor: '#7c3aed'
    });
}

// === INIZIALIZZAZIONE E CLEANUP ===

/**
 * Cleanup quando si cambia pagina
 */
function cleanup() {
    clearInterval(autoRefreshInterval);
    currentSaloneId = null;
    availableServices = [];
    activePromotions = [];
    hideLoading();
}

/**
 * Auto-refresh delle statistiche
 */
let autoRefreshInterval = null;

function startAutoRefresh() {
    if (autoRefreshInterval) {
        clearInterval(autoRefreshInterval);
    }

    // Refresh ogni 5 minuti
    autoRefreshInterval = setInterval(() => {
        if (currentSaloneId && !document.hidden) {
            loadActivePromotions();
        }
    }, 300000);
}

// === EVENT LISTENERS GLOBALI ===

// Inizializzazione al caricamento della pagina
document.addEventListener('DOMContentLoaded', function () {
    // Auto-start se ci sono dati nel DOM
    const saloneIdElement = document.querySelector('[data-salone-id]');
    if (saloneIdElement) {
        const saloneId = saloneIdElement.dataset.saloneId;
        initPromotions(saloneId);
        startAutoRefresh();
    }

    // Setup global error handler
    window.addEventListener('error', function (e) {
        console.error('Errore JavaScript globale:', e.error);
    });
});

// Cleanup al cambio pagina
window.addEventListener('beforeunload', cleanup);

// Gestione visibilità pagina per auto-refresh
document.addEventListener('visibilitychange', function () {
    if (document.hidden) {
        clearInterval(autoRefreshInterval);
    } else if (currentSaloneId) {
        startAutoRefresh();
    }
});

// === ESPORTAZIONE PER COMPATIBILITÀ ===

// Rendi disponibili le funzioni principali globalmente
window.PromotionManager = {
    init: initPromotions,
    toggle: togglePromotion,
    save: savePromotion,
    disable: disablePromotion,
    extend: extendPromotion,
    renew: renewPromotion,
    renewAll: renewAllExpired,
    openBulk: openBulkPromotionModal,
    export: exportPromotionsData,
    generateReport: generatePromotionReport,
    monitor: monitorPromotionPerformance,
    cleanup: cleanup
};

// Debug helper (solo in development)
if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
    window.PromotionDebug = {
        currentSaloneId,
        availableServices,
        activePromotions,
        getState: () => ({
            saloneId: currentSaloneId,
            servicesCount: availableServices.length,
            activePromotionsCount: activePromotions.length
        })
    };
}

console.log('Sistema Promozioni caricato completamente ✅');