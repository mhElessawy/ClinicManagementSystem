(() => {
    const picker = document.getElementById('diagnosis-template-picker');
    if (!picker) return;
    const options = document.getElementById('diagnosis-template-options');
    const status = document.getElementById('diagnosis-template-status');
    const doctor = document.getElementById('DoctorId');
    const details = document.getElementById('DiagnosisDetails');
    if (!details) return;
    let checkboxes = [];
    let request = 0;
    const lines = () => details.value.split(/\r?\n/);
    function syncSelection() {
        const selected = new Set(lines().map(line => line.trim()));
        checkboxes.forEach(input => { input.checked = selected.has(input.value); });
    }
    function changeSelection(input) {
        const current = lines();
        let value;
        if (input.checked) {
            if (current.some(line => line.trim() === input.value)) return;
            value = details.value + (details.value && !details.value.endsWith('\n') ? '\n' : '') + input.value;
        } else {
            value = current.filter(line => line.trim() !== input.value).join('\n');
        }
        if (value.length > 1000) {
            syncSelection();
            status.textContent = 'This would exceed the 1,000 character diagnosis limit. Shorten the diagnosis before selecting more options.';
            return;
        }
        details.value = value;
        details.dispatchEvent(new Event('input', { bubbles: true }));
        status.textContent = 'Selections updated in Diagnosis Details. Save the diagnosis to keep your changes.';
    }
    function render(templates) {
        templates.forEach(template => {
            const choices = [...new Set(template.content.split(/\r?\n/).map(line => line.trim()).filter(Boolean))];
            if (!choices.length) return;
            const group = document.createElement('fieldset');
            group.className = 'mb-3';
            const legend = document.createElement('legend');
            legend.className = 'fs-6 fw-bold';
            legend.textContent = template.name;
            group.append(legend);
            const row = document.createElement('div');
            row.className = 'd-flex flex-wrap gap-3';
            choices.forEach((choice, index) => {
                const label = document.createElement('label');
                label.className = 'form-check d-flex align-items-start gap-2 mb-0';
                const input = document.createElement('input');
                input.type = 'checkbox';
                input.className = 'form-check-input flex-shrink-0';
                input.id = `diagnosis-choice-${template.id}-${index}`;
                input.value = choice;
                // Choices are saved as text in DiagnosisDetails, alongside the doctor's notes.
                input.addEventListener('change', () => changeSelection(input));
                const text = document.createElement('span');
                text.className = 'form-check-label';
                text.style.overflowWrap = 'anywhere';
                text.textContent = choice;
                label.append(input, text);
                row.append(label);
                checkboxes.push(input);
            });
            group.append(row);
            options.append(group);
        });
        syncSelection();
    }
    async function load() {
        const current = ++request;
        checkboxes = [];
        options.replaceChildren();
        options.setAttribute('aria-busy', 'true');
        status.textContent = 'Loading diagnosis options...';
        try {
            const url = new URL(picker.dataset.url, window.location.origin);
            if (doctor?.value) url.searchParams.set('doctorId', doctor.value);
            const response = await fetch(url, { headers: { Accept: 'application/json' } });
            if (!response.ok) throw new Error('Unable to load options.');
            const templates = await response.json();
            if (current !== request) return;
            render(templates);
            status.textContent = checkboxes.length ? 'Options are specific to your clinic and specialty.' : 'No active diagnosis options for this clinic and specialty. Ask your clinic manager to add a template.';
        } catch {
            if (current !== request) return;
            status.textContent = 'Could not load diagnosis options. You can still enter the diagnosis manually. Change the doctor or reload to retry.';
        } finally {
            if (current === request) options.setAttribute('aria-busy', 'false');
        }
    }
    details.addEventListener('input', syncSelection);
    doctor?.addEventListener('change', load);
    load();
})();
