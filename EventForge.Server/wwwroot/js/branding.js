// Preview logo on file select
function previewLogo(input, previewId) {
    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = function(e) {
            document.getElementById(previewId).src = e.target.result;
        };
        reader.readAsDataURL(input.files[0]);
    }
}

// Load tenant branding via AJAX
async function loadTenantBranding(tenantId) {
    if (!tenantId) {
        document.getElementById('tenantBrandingForm').style.display = 'none';
        return;
    }

    document.getElementById('tenantBrandingForm').style.display = 'block';
    document.getElementById('selectedTenantId').value = tenantId;

    try {
        const response = await fetch(`/api/v1/branding?tenantId=${tenantId}`);
        const branding = await response.json();

        // Update preview
        const preview = document.getElementById('tenantPreview');
        preview.innerHTML = `
            <div class="border rounded p-4 mb-3" style="background: #f8f9fa;">
                <img id="tenantLogoPreview" src="${branding.logoUrl}" height="${branding.logoHeight}" alt="Tenant Logo" class="mb-2" />
                <h5>${branding.applicationName}</h5>
                ${branding.isTenantOverride ? '<span class="badge bg-info">Custom</span>' : '<span class="badge bg-secondary">Globale</span>'}
            </div>
        `;

        // Update tenant application name field if available
        const tenantAppNameField = document.getElementById('tenantApplicationName');
        if (tenantAppNameField && branding.isTenantOverride) {
            // Only set if it's different from global
            tenantAppNameField.value = branding.applicationName;
        }
    } catch (error) {
        console.error('Error loading tenant branding:', error);
        document.getElementById('tenantPreview').innerHTML = '<p class="text-danger">Errore nel caricamento</p>';
    }
}

// Reset tenant branding
async function resetTenantBranding() {
    const tenantId = document.getElementById('selectedTenantId').value;
    if (!tenantId) return;

    if (!confirm('Confermi di voler resettare il branding del tenant al globale?')) {
        return;
    }

    try {
        const response = await fetch(`/api/v1/branding/tenant/${tenantId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            alert('Branding tenant resettato con successo!');
            window.location.reload();
        } else {
            alert('Errore durante il reset');
        }
    } catch (error) {
        console.error('Error resetting tenant branding:', error);
        alert('Errore durante il reset');
    }
}
