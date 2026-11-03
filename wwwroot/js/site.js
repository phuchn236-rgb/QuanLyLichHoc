// ===== Khung giờ 12 tiết (45 phút/tiết). Phải khớp với TkbHelper.Tiets trong C# =====
window.TKB_TIMES = [['07:00', '07:45'], ['07:50', '08:35'], ['08:40', '09:25'], ['09:35', '10:20'], ['10:25', '11:10'], ['11:15', '12:00'],
                    ['13:00', '13:45'], ['13:50', '14:35'], ['14:40', '15:25'], ['15:35', '16:20'], ['16:25', '17:10'], ['17:15', '18:00']];
window.tkbRange = function (bd, so) {
    var end = bd + so - 1;
    if (!(bd >= 1 && so >= 1 && end <= 12)) return null;
    return window.TKB_TIMES[bd - 1][0] + ' – ' + window.TKB_TIMES[end - 1][1];
};

// ===== Tiện ích chung =====
function showToast(msg, ok) {
    var el = document.getElementById('appToast');
    el.className = 'toast align-items-center border-0 ' + (ok === false ? 'text-bg-danger' : 'text-bg-success');
    $('#toastMsg').text(msg);
    bootstrap.Toast.getOrCreateInstance(el, { delay: 3000 }).show();
}

// ===== Danh sách lọc + phân trang bằng AJAX =====
var AjaxList = {
    load: function ($box, page) {
        page = page || 1;
        $box.data('page', page);
        var data = $($box.data('form')).serializeArray();
        data.push({ name: 'page', value: page });
        $.get($box.data('url'), data, function (html) { $box.find('.list-container').first().html(html); });
    },
    reload: function ($box) { AjaxList.load($box, $box.data('page') || 1); }
};

$(function () {
    // Gửi token CSRF kèm mọi yêu cầu AJAX
    var token = $('#afForm input[name="__RequestVerificationToken"]').val();
    $.ajaxSetup({ cache: false, headers: { 'RequestVerificationToken': token } });

    $(document).on('submit', '[data-ajax-list] form', function (e) {
        e.preventDefault();
        AjaxList.load($(this).closest('[data-ajax-list]'), 1);
    });
    $(document).on('change', '[data-ajax-list] form select', function () {
        AjaxList.load($(this).closest('[data-ajax-list]'), 1);
    });
    $(document).on('click', '.pager-link', function (e) {
        e.preventDefault();
        if ($(this).parent().hasClass('disabled') || $(this).parent().hasClass('active')) return;
        AjaxList.load($(this).closest('[data-ajax-list]'), $(this).data('page'));
    });

    // Admin / Giáo vụ: xem danh sách sinh viên đã đăng ký một lớp học phần
    $(document).on('click', '.btn-enrolled', function () {
        var $m = $('#enrolledModal'); if (!$m.length) return;
        $('#enrolledBody').html('<div class="text-center text-muted py-4">Đang tải…</div>');
        bootstrap.Modal.getOrCreateInstance($m[0]).show();
        $.get('/Home/Enrolled', { maLHP: $(this).data('lhp'), maHK: $(this).data('hk') || 0 }, function (html) { $('#enrolledBody').html(html); })
         .fail(function () { $('#enrolledBody').html('<div class="alert alert-danger">Không tải được danh sách.</div>'); });
    });

    // ===== CRUD bằng modal =====
    var $crud = $('#crud');
    if (!$crud.length) return;
    var ctrl = $crud.data('controller');
    var modal = new bootstrap.Modal(document.getElementById('crudModal'));
    var checkTimer = null;

    function showErrors(list) { $('#formErrors').removeClass('d-none').html(list.join('<br>')); }

    function openForm(id) {
        $.get('/' + ctrl + '/Form', id ? { id: id } : {}, function (html) {
            $('#crudFields').html(html);
            $('#isNew').val(id ? 'false' : 'true');
            $('#formErrors').addClass('d-none').empty();
            modal.show();
            if ($('#checkInfo').length) runCheck();
            updateTimeHint();
        }).fail(function () { showToast('Không mở được biểu mẫu.', false); });
    }

    $('#btnAdd').on('click', function () { openForm(null); });
    $(document).on('click', '.btn-edit', function () { openForm($(this).data('id')); });

    $('#btnSave').on('click', function () {
        $.post('/' + ctrl + '/Save', $('#crudForm').serialize(), function (r) {
            if (r.ok) { modal.hide(); AjaxList.reload($crud); showToast(window.I18N.saved); }
            else showErrors(r.errors);
        }).fail(function () { showErrors(['Có lỗi xảy ra, vui lòng thử lại.']); });
    });

    $(document).on('click', '.btn-del', function () {
        if (!confirm(window.I18N.confirmDelete)) return;
        $.post('/' + ctrl + '/Delete', { id: $(this).data('id') }, function (r) {
            if (r.ok) { AjaxList.reload($crud); showToast(window.I18N.deleted); }
            else showToast(r.errors.join(' '), false);
        });
    });


    // Gợi ý giờ học thực tế khi xếp lịch (Giáo vụ / Admin)
    function updateTimeHint() {
        var $h = $('#timeHint'); if (!$h.length) return;
        var bd = parseInt($('#crudForm [name="TietBatDau"]').val(), 10), so = parseInt($('#crudForm [name="SoTiet"]').val(), 10);
        var r = window.tkbRange(bd, so);
        if (!r) { $h.removeClass('alert-light border alert-success').addClass('alert-warning').html('<i class="bi bi-exclamation-triangle"></i> Tiết học vượt quá tiết 12.'); return; }
        if (bd <= 6 && bd + so - 1 > 6) { $h.removeClass('alert-light border alert-success').addClass('alert-warning').html('<i class="bi bi-exclamation-triangle"></i> Buổi học không được kéo dài từ sáng (tiết 1–6) sang chiều (tiết 7–12).'); return; }
        $h.removeClass('alert-warning').addClass('alert-light border').html('<i class="bi bi-clock"></i> Tiết ' + bd + '–' + (bd + so - 1) + ': <b>' + r + '</b> (' + (bd <= 6 ? 'buổi sáng' : 'buổi chiều') + ')');
    }
    $(document).on('input change', '#crudForm [name="TietBatDau"], #crudForm [name="SoTiet"]', updateTimeHint);

    // Kiểm tra xung đột lịch ngay khi người dùng thay đổi trường dữ liệu (AJAX)
    function runCheck() {
        var $info = $('#checkInfo'); if (!$info.length) return;
        $.post($info.data('check-url'), $('#crudForm').serialize(), function (r) {
            if (r.ok) $info.removeClass('alert-warning d-none').addClass('alert-success').html('<i class="bi bi-check-circle"></i> Không phát hiện xung đột.');
            else $info.removeClass('alert-success d-none').addClass('alert-warning').html('<i class="bi bi-exclamation-triangle"></i> ' + r.errors.join('<br>'));
        });
    }
    $(document).on('change input', '#crudForm [data-check]', function () {
        clearTimeout(checkTimer); checkTimer = setTimeout(runCheck, 300);
    });
});
