(() => {
    const picker = document.getElementById('diagnosis-template-picker');
    if (!picker) return;
    const select = document.getElementById('diagnosis-template');
    const button = document.getElementById('insert-diagnosis-template');
    const preview = document.getElementById('diagnosis-template-preview');
    const status = document.getElementById('diagnosis-template-status');
    const doctor = document.getElementById('DoctorId');
    const details = document.getElementById('DiagnosisDetails');
    let templates = [];
    let request = 0;
    async function load() {
        const current = ++request;
        templates = [];
        select.replaceChildren(new Option('Loading templates...', ''));
        select.disabled = button.disabled = true;
        preview.textContent = '';
        try {
            const url = new URL(picker.dataset.url, window.location.origin);
            if (doctor?.value) url.searchParams.set('doctorId', doctor.value);
            const response = await fetch(url, { headers: { Accept: 'application/json' } });
            if (!response.ok) throw new Error('Unable to load templates.');
            const data = await response.json();
            if (current !== request) return;
            templates = data;
            select.replaceChildren(new Option(data.length ? '-- Select Template --' : 'No templates available', ''));
            data.forEach(t => select.add(new Option(t.name, String(t.id))));
            select.disabled = !data.length;
            status.textContent = data.length ? 'Preview a template, then insert it into the diagnosis. Existing text is kept.' : 'No active templates for this clinic and specialty. Ask your clinic manager to add one.';
        } catch {
            if (current !== request) return;
            select.replaceChildren(new Option('Templates unavailable', ''));
            status.textContent = 'Could not load templates. You can still enter the diagnosis manually. Change the doctor or reload to retry.';
        }
    }
    select.addEventListener('change', () => {
        const template = templates.find(t => String(t.id) === select.value);
        preview.textContent = template?.content || '';
        button.disabled = !template;
    });
    button.addEventListener('click', () => {
        const template = templates.find(t => String(t.id) === select.value);
        if (!template || !details) return;
        const value = details.value ? details.value + '\n\n' + template.content : template.content;
        if (value.length > 1000) {
            status.textContent = 'This would exceed the 1,000 character diagnosis limit. Shorten the existing text before inserting.';
            return;
        }
        details.value = value;
        details.dispatchEvent(new Event('input', { bubbles: true }));
        details.focus();
        status.textContent = 'Template inserted. Review and edit the diagnosis before saving.';
    });
    doctor?.addEventListener('change', load);
    load();
})();
