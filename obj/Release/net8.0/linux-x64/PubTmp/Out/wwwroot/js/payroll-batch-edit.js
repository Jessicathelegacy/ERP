document.addEventListener('DOMContentLoaded', function () {

    // Restore active tab after form submit (reads value set by server via hidden input)
    const activeTabInput = document.getElementById('activeTab');
    if (activeTabInput && activeTabInput.value) {
        const tabBtn = document.querySelector(`[data-bs-target="#${activeTabInput.value}"]`);
        if (tabBtn) bootstrap.Tab.getOrCreateInstance(tabBtn).show();
    }

    // Live recalculation when OT hours change
    document.querySelectorAll('.ot-input').forEach(function (input) {
        input.addEventListener('input', function () {
            const tr         = this.closest('tr');
            const basic      = parseFloat(this.dataset.basic)      || 0;
            const allowances = parseFloat(this.dataset.allowances)  || 0;
            const rate       = parseFloat(this.dataset.rate)        || 0;
            const hours      = parseFloat(this.value)               || 0;
            const otPay      = Math.round(hours * rate * 100) / 100;
            const gross      = basic + otPay + allowances;
            const ded        = parseFloat(tr.querySelector('.deduction-input').value) || 0;

            tr.querySelector('.ot-pay-cell').textContent = otPay.toFixed(2);
            tr.querySelector('.gross-cell').textContent  = gross.toFixed(2);
            tr.querySelector('.net-cell').textContent    = (gross - ded).toFixed(2);
            recalcFooter();
        });
    });

    // Live recalculation when deductions change
    document.querySelectorAll('.deduction-input').forEach(function (input) {
        input.addEventListener('input', function () {
            const tr    = this.closest('tr');
            const gross = parseFloat(tr.querySelector('.gross-cell').textContent) || 0;
            const ded   = parseFloat(this.value) || 0;
            tr.querySelector('.net-cell').textContent = (gross - ded).toFixed(2);
            recalcFooter();
        });
    });

    function recalcFooter() {
        let totalOtPay = 0, totalGross = 0, totalDed = 0, totalNet = 0;
        document.querySelectorAll('#deductionsTable tbody tr').forEach(function (tr) {
            totalOtPay += parseFloat(tr.querySelector('.ot-pay-cell').textContent)   || 0;
            totalGross += parseFloat(tr.querySelector('.gross-cell').textContent)    || 0;
            totalDed   += parseFloat(tr.querySelector('.deduction-input').value)     || 0;
            totalNet   += parseFloat(tr.querySelector('.net-cell').textContent)      || 0;
        });
        document.getElementById('totalOtPay').textContent      = totalOtPay.toFixed(2);
        document.getElementById('totalGross').textContent      = totalGross.toFixed(2);
        document.getElementById('totalDeductions').textContent = totalDed.toFixed(2);
        document.getElementById('totalNet').textContent        = totalNet.toFixed(2);
    }
});
