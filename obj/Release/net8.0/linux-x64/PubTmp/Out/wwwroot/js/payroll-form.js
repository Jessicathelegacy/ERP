let otRate = 0;

function recalculate() {
    const basic = parseFloat(document.getElementById('BasicSalary').value) || 0;
    const ot    = parseFloat(document.getElementById('OvertimePay').value)  || 0;
    const allow = parseFloat(document.getElementById('Allowances').value)   || 0;
    const ded   = parseFloat(document.getElementById('Deductions').value)   || 0;
    const gross = basic + ot + allow;
    document.getElementById('GrossSalary').value = gross.toFixed(2);
    document.getElementById('NetSalary').value   = (gross - ded).toFixed(2);
}

async function loadEmployeeDefaults(employeeId, fillValues) {
    if (!employeeId || employeeId === '0') return;
    try {
        const resp = await fetch(`/Payroll/GetEmployeeDefaults?employeeId=${employeeId}`);
        if (!resp.ok) return;
        const data = await resp.json();
        otRate = data.overtimeRatePerHour;
        if (fillValues) {
            document.getElementById('BasicSalary').value  = data.basicSalary.toFixed(2);
            document.getElementById('Allowances').value   = data.allowances.toFixed(2);
            document.getElementById('OvertimeHours').value = '0';
            document.getElementById('OvertimePay').value  = '0.00';
            document.getElementById('Deductions').value   = '0.00';
            recalculate();
        }
    } catch { }
}

document.addEventListener('DOMContentLoaded', function () {
    const initialEmployeeId = document.getElementById('EmployeeId').value;
    loadEmployeeDefaults(initialEmployeeId, false);

    document.getElementById('EmployeeId').addEventListener('change', function () {
        loadEmployeeDefaults(this.value, true);
    });

    document.getElementById('OvertimeHours').addEventListener('input', function () {
        const hrs = parseFloat(this.value) || 0;
        document.getElementById('OvertimePay').value = (hrs * otRate).toFixed(2);
        recalculate();
    });

    ['BasicSalary', 'OvertimePay', 'Allowances', 'Deductions'].forEach(id => {
        document.getElementById(id).addEventListener('input', recalculate);
    });
});
