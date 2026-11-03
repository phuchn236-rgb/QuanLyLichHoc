// Lõi dựng lưới thời khóa biểu (12 tiết x 7 ngày) dùng chung cho nhiều trang
window.Tkb = (function () {
    var DAYS = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'CN'];
    var TIMES = window.TKB_TIMES;   // định nghĩa ở site.js (45 phút/tiết)

    function esc(s) { return $('<div>').text(s || '').html(); }
    function color(s) { var h = 0; s = s || ''; for (var i = 0; i < s.length; i++) h = (h * 31 + s.charCodeAt(i)) % 360; return 'hsl(' + h + ',70%,85%)'; }
    function fmt(iso) { var p = iso.split('-'); return p[2] + '/' + p[1] + '/' + p[0]; }
    function cell($g, thu, tiet) { return $g.find('[data-c="' + thu + '_' + tiet + '"]'); }

    function build($g) {
        var h = '<thead><tr><th style="width:92px">Tiết</th>';
        DAYS.forEach(function (d) { h += '<th>' + d + '</th>'; });
        h += '</tr></thead><tbody>';
        for (var t = 1; t <= 12; t++) {
            h += '<tr><th>' + t + '<br><small class="text-muted">' + TIMES[t - 1][0] + '–' + TIMES[t - 1][1] + '</small></th>';
            for (var d = 2; d <= 8; d++) h += '<td data-c="' + d + '_' + t + '"></td>';
            h += '</tr>';
        }
        $g.html(h + '</tbody>');
    }

    // Kiểm tra mọi ô trong khoảng tiết còn trống
    function isFree($g, b) {
        for (var k = 0; k < b.soTiet; k++) {
            var $c = cell($g, b.thu, b.tietBatDau + k);
            if (!$c.length || $c.hasClass('slot-busy')) return false;
        }
        return true;
    }

    // Đặt các buổi học lên lưới. opt.preview = true: hiển thị dạng xem trước (nét đứt)
    function fill($g, items, opt) {
        opt = opt || {};
        var compact = $g.hasClass('tkb-mini');
        // Trong thời khóa biểu của chính giảng viên không cần lặp lại tên giảng viên: hiển thị mã lớp học phần
        var line3 = function (b) { return opt.hideGv ? esc(b.maLHP) : esc(b.giangVien); };
        items.forEach(function (b) {
            if (!isFree($g, b)) {
                for (var k = 0; k < b.soTiet; k++) cell($g, b.thu, b.tietBatDau + k).addClass('slot-conflict');
                return;
            }
            var $c = cell($g, b.thu, b.tietBatDau);
            var html = compact
                ? '<b>' + esc(b.tenMon) + '</b><br>' + esc(b.phong)
                : '<b>' + esc(b.tenMon) + '</b><br>' + esc(b.phong) + '<br><small>' + line3(b) + '<br>' + esc(b.thoiGian) + '</small>';
            $c.attr('rowspan', b.soTiet).addClass('slot-busy').html(html);
            if (opt.preview) $c.addClass('slot-preview'); else $c.css('background', color(b.maMH || b.maLHP));
            for (var k = 1; k < b.soTiet; k++) cell($g, b.thu, b.tietBatDau + k).remove();
        });
    }

    return { build: build, fill: fill, fmt: fmt, esc: esc };
})();
