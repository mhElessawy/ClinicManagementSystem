const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const source = fs.readFileSync(path.join(__dirname, '../wwwroot/js/diagnosis-templates.js'), 'utf8');

class Element extends EventTarget {
    constructor(tag = 'div') {
        super();
        this.tag = tag;
        this.children = [];
        this.style = {};
        this.dataset = {};
        this.value = '';
        this.checked = false;
        this.textContent = '';
    }
    append(...children) { this.children.push(...children); }
    replaceChildren(...children) { this.children = children; }
    setAttribute(name, value) { this[name] = value; }
}
const templates = [
    { id: 1, name: 'Symptoms', content: ' Cough\r\nFever\r\n\r\nCough' },
    { id: 2, name: 'Examination', content: 'Fever\nNormal examination' }
];
async function setup(value = '', fetchResult = templates) {
    const elements = Object.fromEntries(['diagnosis-template-picker', 'diagnosis-template-options',
        'diagnosis-template-status', 'DoctorId', 'DiagnosisDetails'].map(id => [id, new Element()]));
    elements['diagnosis-template-picker'].dataset.url = '/PatientDiagnoses/Templates';
    elements.DiagnosisDetails.value = value;
    elements.DoctorId.value = '7';
    let requestedUrl;
    vm.runInNewContext(source, {
        document: { getElementById: id => elements[id], createElement: tag => new Element(tag) },
        window: { location: { origin: 'https://localhost:7001' } }, URL, Event,
        fetch: async url => {
            requestedUrl = url;
            if (fetchResult instanceof Error) throw fetchResult;
            return { ok: true, json: async () => fetchResult };
        }
    });
    await new Promise(resolve => setImmediate(resolve));
    const walk = node => [node, ...node.children.flatMap(walk)];
    const inputs = () => walk(elements['diagnosis-template-options']).filter(node => node.tag === 'input');
    const toggle = (input, checked) => { input.checked = checked; input.dispatchEvent(new Event('change')); };
    return { elements, inputs, toggle, requestedUrl };
}

test('Each nonblank unique line becomes a checkbox in its template group', async () => {
    const { inputs, elements, requestedUrl } = await setup();
    assert.deepEqual(inputs().map(input => input.value), ['Cough', 'Fever', 'Fever', 'Normal examination']);
    assert.equal(elements['diagnosis-template-options'].children[0].children[0].textContent, 'Symptoms');
    assert.equal(requestedUrl.searchParams.get('doctorId'), '7');
});
test('Checking multiple options and unchecking preserves the doctor notes', async () => {
    const { inputs, elements, toggle } = await setup('Doctor notes');
    toggle(inputs()[0], true);
    toggle(inputs()[1], true);
    assert.equal(elements.DiagnosisDetails.value, 'Doctor notes\nCough\nFever');
    assert.equal(inputs()[2].checked, true);
    toggle(inputs()[2], false);
    assert.equal(elements.DiagnosisDetails.value, 'Doctor notes\nCough');
    assert.equal(inputs()[1].checked, false);
});
test('Saved choices are checked on edit; manually edited notes synchronize choices', async () => {
    const { inputs, elements, toggle } = await setup('Notes about Cough\r\nFever');
    assert.equal(inputs()[0].checked, false);
    assert.equal(inputs()[1].checked, true);
    toggle(inputs()[1], false);
    assert.equal(elements.DiagnosisDetails.value, 'Notes about Cough');
    elements.DiagnosisDetails.value += '\nNormal examination';
    elements.DiagnosisDetails.dispatchEvent(new Event('input'));
    assert.equal(inputs()[3].checked, true);
});
test('A selection beyond the diagnosis limit is rejected without losing text', async () => {
    const { inputs, elements, toggle } = await setup('x'.repeat(998));
    toggle(inputs()[0], true);
    assert.equal(inputs()[0].checked, false);
    assert.equal(elements.DiagnosisDetails.value, 'x'.repeat(998));
    assert.match(elements['diagnosis-template-status'].textContent, /1,000/);
});
test('Unavailable options leave manual diagnosis usable', async () => {
    const { elements, inputs } = await setup('Saved notes', new Error('offline'));
    assert.equal(inputs().length, 0);
    assert.equal(elements.DiagnosisDetails.value, 'Saved notes');
    assert.match(elements['diagnosis-template-status'].textContent, /manually/);
    assert.equal(elements['diagnosis-template-options']['aria-busy'], 'false');
});
